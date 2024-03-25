using Life;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;

namespace TrollBlocker
{
    public class TrollBlocker : ModKit.ModKit
    {
        private readonly Events events;

        public TrollBlocker(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
            events = new Events(api);
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }
    }
}
