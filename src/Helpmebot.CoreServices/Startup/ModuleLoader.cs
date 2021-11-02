namespace Helpmebot.CoreServices.Startup
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using Castle.Windsor.Installer;
    using Helpmebot.Configuration;
    using Stwalkerster.Bot.CommandLib.Commands.Interfaces;

    public class ModuleLoader
    {
        private readonly ModuleConfiguration moduleList;
        private readonly List<Assembly> loadedAssemblies = new List<Assembly>();
        
        public ModuleLoader(ModuleConfiguration moduleList)
        {
            this.moduleList = moduleList;
        }

        public void LoadModuleAssemblies()
        {
            foreach (var module in this.moduleList.Modules)
            {
                var assembly = Assembly.LoadFile(Path.GetFullPath(module.Assembly));
                this.loadedAssemblies.Add(assembly);
            }
        }

        public void InstallModuleConfiguration(IWindsorContainer container)
        {
            var botConfiguration = container.Resolve<BotConfiguration>();

            foreach (var module in this.moduleList.Modules.Where(x => !string.IsNullOrWhiteSpace(x.CastleFile)))
            {
                var configFileName = module.CastleFile;
                var configPath = "Configuration" + Path.DirectorySeparatorChar + configFileName;
                if (!string.IsNullOrWhiteSpace(botConfiguration.ModuleConfigurationPath))
                {
                    var customConfigPath = botConfiguration.ModuleConfigurationPath + Path.DirectorySeparatorChar + configFileName;

                    if (File.Exists(customConfigPath))
                    {
                        configPath = customConfigPath;
                    }
                }
                
                container.Install(Configuration.FromXmlFile(configPath));
            }
        }
        
        public void InstallModules(IWindsorContainer container)
        {
            foreach (var assembly in this.loadedAssemblies)
            {
                var ns = assembly.FullName.Split(',')[0];
                
                container.Register(
                    Classes.FromAssembly(assembly).BasedOn<ICommand>().LifestyleTransient(),            
                    Classes.FromAssembly(assembly).InNamespace(ns + ".Services").WithServiceAllInterfaces()
                );
            }
        }
    }
}