using EldritchGames.EldritchLogger.Console.Themes;
using NUnit.Framework;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ResourcesThemeLoaderTests
    {
        [Test]
        public void ThemeLoader_FindsPackageThemes_CaseInsensitively()
        {
            var loader = new ResourcesThemeLoader();

            Assert.That(loader.LoadAllThemes().Select(t => t.name), Does.Contain("Dark"));
            Assert.That(loader.LoadTheme("solarized dark")?.name, Is.EqualTo("Solarized Dark"));
            Assert.That(loader.LoadTheme(""), Is.Null);
            Assert.That(loader.LoadTheme("Neon"), Is.Null);
        }
    }
}
