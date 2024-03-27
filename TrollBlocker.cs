using Life;
using ModKit.Helper;
using _menu = AAMenu.Menu;
using ModKit.Interfaces;
using Life.Network;
using Life.UI;
using TrollBlocker.Entities;
using Life.DB;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ModKit.Utils;

namespace TrollBlocker
{
    public class TrollBlocker : ModKit.ModKit
    {
        public static string ConfigDirectoryPath;
        public static string ConfigJailFilePath;

        private readonly Events events;
        public static System.Random rand;

        public static List<Player> BlackList = new List<Player>();
        public static List<TrollBlockerJail> Jails = new List<TrollBlockerJail>();

        public static JailConfig JailConfig { get; set;}

        public TrollBlocker(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
            events = new Events(api);
            rand = new System.Random();

            new SChatCommand("/trollblocker",
               "mise à jour avec le fichier config",
               "/trollblocker",
               (player, arg) => {
                   JailConfig = LoadConfigFile(ConfigJailFilePath);
               }).Register();
        }

        public async override void OnPluginInit()
        {
            base.OnPluginInit();
            InitEntities();
            InitDirectoryAndFiles();
            JailConfig = LoadConfigFile(ConfigJailFilePath);
            InsertMenu();
            events.Init(Nova.server);

            Jails = await TrollBlockerJail.QueryAll();
            ModKit.Internal.Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        private void InitDirectoryAndFiles()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/TrollBlocker";
                ConfigJailFilePath = System.IO.Path.Combine(ConfigDirectoryPath, "jail.json");

                if (!Directory.Exists(ConfigDirectoryPath)) Directory.CreateDirectory(ConfigDirectoryPath);
                if (!File.Exists(ConfigJailFilePath)) InitJailFile();
            }
            catch (IOException ex)
            {
                ModKit.Internal.Logger.LogError("InitDirectory", ex.Message);
            }
        }
        private void InitJailFile()
        {
            JailConfig jailConfig = new JailConfig
            {
                areaId = 1,
                VPosition = new Vector3 (0f, 0f, 0f)
            };

            string jailJson = JsonConvert.SerializeObject(jailConfig, Formatting.Indented);
            File.WriteAllText(ConfigJailFilePath, jailJson);
        }
        private JailConfig LoadConfigFile(string path)
        {
            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);
                JailConfig jailConfig = JsonConvert.DeserializeObject<JailConfig>(jsonContent);

                return jailConfig;
            }
            else return null;
        }

        public void InitEntities()
        {
            Orm.RegisterTable<TrollBlockerPlayer>();
            Orm.RegisterTable<TrollBlockerJail>();
        }

        public void InsertMenu()
        {
            _menu.AddAdminTabLine(PluginInformations, 2, "Bloquer un Troll", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                if(player.IsAdmin) OpenTrollBlockerPanel(player);
            });
        }

        #region PANELS
        public void OpenTrollBlockerPanel(Player player)
        {
            Panel panel = PanelHelper.Create($"Prison des trolls", UIPanel.PanelType.Text, player, () => OpenTrollBlockerPanel(player));

            panel.TextLines.Add("Cette prison est destinée aux joueurs HRP.");
            panel.TextLines.Add("C'est une manière d'accorder une seconde chance");
            panel.TextLines.Add("plutôt que de bannir définitivement.");

            panel.NextButton("Prisonniers", () => TrollBlockerPlayerPanel(player));
            panel.NextButton("Cellules", () => TrollBlockerJailPanel(player));
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        public async void TrollBlockerPlayerPanel(Player player)
        {
            var prisoners = await TrollBlockerPlayer.QueryAll();

            Panel panel = PanelHelper.Create($"Prisonniers", UIPanel.PanelType.Tab, player, () => TrollBlockerPlayerPanel(player));

            foreach(var prisoner in prisoners)
            {
                if (prisoner.IsActive)
                {
                    panel.AddTabLine($"Prisonnier n°{prisoner.PlayerId}", async ui => {
                        prisoner.IsActive = false;
                        if (await prisoner.Save())
                        {                                              
                            var target = Nova.server.Players.Where(p => p.account.id == prisoner.PlayerId).FirstOrDefault();
                            BlackList.Remove(target);
                            if (target != default && target != null) target.setup.TargetSetPosition(JailConfig.VPosition);
                            panel.Refresh();
                        }
                        else player.Notify("Erreur", "Nous n'avons pas pu libérer ce prisonnier", NotificationManager.Type.Error);
                    });
                }
            }

            panel.NextButton("Ajouter", () => TrollBlockerAddPlayerPanel(player));
            panel.AddButton("Libérer", ui => ui.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        public void TrollBlockerAddPlayerPanel(Player player)
        {
            Panel panel = PanelHelper.Create($"Ajouter un prisonnier", UIPanel.PanelType.Input, player, () => TrollBlockerAddPlayerPanel(player));

            panel.TextLines.Add("Indiquer l'ID du joueur à enfermer.");

            panel.PreviousButtonWithAction("Enfermer", async () =>
            {
                if(panel.inputText.Length > 0 && int.TryParse(panel.inputText, out int accountId))
                {
                    var target = await LifeDB.FetchAccount(accountId);
                    if(target != null || target != default)
                    {
                        var prisoner = new TrollBlockerPlayer();
                        prisoner.PlayerId = target.id;
                        prisoner.IsActive = true;
                        prisoner.CreatedAt = DateUtils.GetNumericalDateOfTheDay();
                        if (await prisoner.Save())
                        {
                            var currentPlayer = Nova.server.Players.Where(p => p.account.id == prisoner.PlayerId).FirstOrDefault();
                            if (currentPlayer != default && currentPlayer != null)
                            {
                                int randomIndex = rand.Next(0, Jails.Count);
                                var jail = Jails[randomIndex];
                                currentPlayer.setup.TargetSetPosition(jail.VPosition);
                                BlackList.Add(currentPlayer);
                            }

                            player.Notify("Succès", $"Prisonnier n°{prisoner.PlayerId} affecté à la prison des trolls", NotificationManager.Type.Success);
                            return await Task.FromResult(true);
                        }
                        else
                        {
                            player.Notify("Erreur", $"Nous n'avons pas pu enfermer votre cible", NotificationManager.Type.Error);
                            return await Task.FromResult(false);
                        }
                    }
                    else
                    {
                        player.Notify("Erreur", "Le compte n'existe pas.", NotificationManager.Type.Error);
                        return await Task.FromResult(false);
                    }
                }
                else
                {
                    player.Notify("Erreur", "Veuillez indiquer l'identifiant du joueur (accountId)", NotificationManager.Type.Error);
                    return await Task.FromResult(false);
                }
                
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        public void TrollBlockerJailPanel(Player player)
        {
            Panel panel = PanelHelper.Create($"Cellules", UIPanel.PanelType.Tab, player, () => TrollBlockerJailPanel(player));

            foreach (var jail in Jails)
            {
                panel.AddTabLine($"{jail.Name}", async ui => {
                    if (await jail.Delete())
                    {
                        Jails.Remove(jail);
                        panel.Refresh();
                    }
                    else player.Notify("Erreur", "Nous n'avons pas pu supprimer cette cellule", NotificationManager.Type.Error);
                });
            }


            panel.NextButton("Ajouter", () => TrollBlockerAddJailPanel(player));
            panel.AddButton("Supprimer", ui => ui.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        public void TrollBlockerAddJailPanel(Player player)
        {
            Panel panel = PanelHelper.Create($"Ajouter une cellule", UIPanel.PanelType.Input, player, () => TrollBlockerAddJailPanel(player));

            panel.TextLines.Add("Nommer votre cellule");

            panel.PreviousButtonWithAction("Enregistrer", async () =>
            {
                if (panel.inputText.Length > 0)
                {
                    TrollBlockerJail newJail = new TrollBlockerJail();
                    newJail.VPosition = player.setup.transform.position;
                    newJail.Name = panel.inputText;


                    if (await newJail.Save())
                    {
                        Jails.Add(newJail);
                        player.Notify("Succès", $"Cellule enregistrée", NotificationManager.Type.Success);
                        return await Task.FromResult(true);
                    }
                    else
                    {
                        player.Notify("Erreur", $"Nous n'avons pas pu enregistrer votre cellule", NotificationManager.Type.Error);
                        return await Task.FromResult(false);
                    }                   
                }
                else
                {
                    player.Notify("Erreur", "Veuillez indiquer le nom de votre cellule", NotificationManager.Type.Error);
                    return await Task.FromResult(false);
                }
               
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        #endregion
    }
}
