using System;
using System.ComponentModel;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;

namespace Bitwarden.AutoType.Desktop.Helpers
{
    public class ServiceLocatorExtension : MarkupExtension
    {
        public Type? ServiceType { get; set; }

        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            if (ServiceType == null)
            {
                throw new InvalidOperationException("ServiceType must be set.");
            }

            if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                // Return design-time instance or default value
                // Example: return new SettingsControl();
                if (ServiceType != null)
                {
                    return Activator.CreateInstance(ServiceType);
                }
                return null;
            }
            else
            {
                var app = System.Windows.Application.Current as App;
                return app!.Host.Services.GetRequiredService(ServiceType);
            }
        }
    }
}