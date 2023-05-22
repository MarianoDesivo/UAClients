using BlazorServer.Data;
using ConsoleClient;
using ConsoleClient.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;
using UnifiedAutomation.UaSchema;


static void Application_UntrustedCertificate(object sender, UntrustedCertificateEventArgs e)
{
    List<StatusCode>
validationErrors = e.ValidationError.Flattened;

    Console.WriteLine("\n");
    Console.WriteLine("-------------------------------------------------------");

    if (validationErrors.Count == 1)
    {
        if (e.ValidationError == UnifiedAutomation.UaBase.StatusCodes.BadCertificateUntrusted)
        {
            Console.WriteLine($" - Untrusted certificate: {e.Certificate}");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(" - Press 0 to accept the certificate                  -");
        }
        else
        {
            Console.WriteLine($" - Validation error in certificate: {e.ValidationError}, {e.Certificate}");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(" - Press 0 to accept the validation error             -");
        }
        Console.WriteLine(" - Press other key to reject the certificate          -");

        ConsoleKeyInfo key = Console.ReadKey();
        switch (key.Key)
        {
            case ConsoleKey.D0:
            case ConsoleKey.NumPad0:
                e.Accept = true;
                e.Persist = true;
                break;
            default:
                break;
        }
    }
    else
    {
        if (e.ValidationError == UnifiedAutomation.UaBase.StatusCodes.BadCertificateUntrusted)
        {
            Console.WriteLine($" - Untrusted certificate: {e.Certificate}");
            Console.WriteLine("-------------------------------------------------------");
        }
        else
        {
            Console.WriteLine($" - Validation error in certificate: {e.ValidationError}, {e.Certificate}");
            Console.WriteLine("-------------------------------------------------------");
        }

        Console.WriteLine(" - Further validation errors:");
        for (int ii = 1; ii < validationErrors.Count; ii++)
        {
            Console.WriteLine($" -   {validationErrors[ii]}");
        }

        if (e.ValidationError == UnifiedAutomation.UaBase.StatusCodes.BadCertificateUntrusted)
        {
            Console.WriteLine(" - Press 0 to trust the certificate                   -");
        }
        else
        {
            Console.WriteLine(" - Press 0 to accept this validation error             -");
        }
        Console.WriteLine(" - Press 1 to accept all validation errors            -");
        Console.WriteLine(" - Press other key to reject the certificate          -");

        ConsoleKeyInfo key = Console.ReadKey();
        switch (key.Key)
        {
            case ConsoleKey.D0:
            case ConsoleKey.NumPad0:
                e.Accept = true;
                e.Persist = true;
                break;
            case ConsoleKey.D1:
            case ConsoleKey.NumPad1:
                e.AcceptAll = true;
                e.Persist = true;
                break;
            default:
                break;
        }
    }
}
//! [UntrustedCertificate]
static UnifiedAutomation.UaSchema.IConfiguration LoadInMemoryConfiguration()
{
    ConfigurationInMemory configuration = new ConfigurationInMemory()
    {
        ApplicationName = "OPC UA .NET Console Client",
        ApplicationType = UnifiedAutomation.UaSchema.ApplicationType.Client_1,
        ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:UnifiedAutomation:ConsoleClient",
        ProductName = "Console Client",
        CertificateCheckSettings = new CertificateCheckSettings()
        {
            Default = new SingleCertificateCheckSettings()
        },
        ClientSettings = new UnifiedAutomation.UaSchema.ClientSettings()
        {
            AutomaticReconnect = true,
            WatchdogCycleTime = 5000,
            WatchdogTimeout = 10000,
            ProductUri = "urn:UnifiedAutomation:ConsoleClient",
        }
    };
    configuration.SetSecurity(PlatformUtils.CombinePath(PlatformUtils.IsWindows() ? "%CommonApplicationData%" : "%LocalApplicationData%", "UnifiedAutomation", "UaSdkNetBundleBinary", "pkiclient"), "CN=ConsoleClient/O=UnifiedAutomation/DC=localhost");

    return configuration;
}

static UnifiedAutomation.UaSchema.TraceSettings TraceSettingsFromConfiguration(IClientConfiguration configuration)
{
    if (configuration.TraceSettings != null)
    {
        return new UnifiedAutomation.UaSchema.TraceSettings()
        {
            MasterTraceEnabled = configuration.TraceSettings.Enabled,
            DefaultTraceLevel = configuration.TraceSettings.TraceLevel,
            TraceFile = PlatformUtils.CombinePath(PlatformUtils.IsWindows() ? "%CommonApplicationData%" : "%LocalApplicationData%", "UnifiedAutomation", "logs", "UaSdkNetBundleBinary", configuration.TraceSettings.TraceFile),
            ModuleSettings = new UnifiedAutomation.UaSchema.ModuleTraceSettings[]
        {
    new UnifiedAutomation.UaSchema.ModuleTraceSettings() { ModuleName = "UnifiedAutomation.Stack" },
    new UnifiedAutomation.UaSchema.ModuleTraceSettings() { ModuleName = "UnifiedAutomation.Client" }
        }
        };
    }
    return new UnifiedAutomation.UaSchema.TraceSettings()
    {
        MasterTraceEnabled = true,
        DefaultTraceLevel = UnifiedAutomation.UaSchema.TraceLevel.ProgramFlow,
        TraceFile = PlatformUtils.CombinePath(PlatformUtils.IsWindows() ? "%CommonApplicationData%" : "%LocalApplicationData%", "UnifiedAutomation", "logs", "UaSdkNetBundleBinary", "ConsoleClient.txt"),
        ModuleSettings = new UnifiedAutomation.UaSchema.ModuleTraceSettings[]
    {
    new UnifiedAutomation.UaSchema.ModuleTraceSettings() { ModuleName = "UnifiedAutomation.Stack" },
    new UnifiedAutomation.UaSchema.ModuleTraceSettings() { ModuleName = "UnifiedAutomation.Client" }
    }
    };
}

static bool TryGetGdsSettings(IClientConfiguration configuration, out UnifiedAutomation.UaSchema.GdsSettings settings, out string gdsFolder)
{
    if (configuration.GdsSettings == null || !configuration.GdsSettings.UseGdsSecurity)
    {
        settings = null;
        gdsFolder = null;
        return false;
    }
    settings = new UnifiedAutomation.UaSchema.GdsSettings()
    {
        GdsDiscoveryUrl = configuration.GdsSettings.GdsDiscoveryUrl,
        IgnoreGdsDefinedUpdateFrequency = configuration.GdsSettings.UpdateFrequency != 0
    };
    if (settings.IgnoreGdsDefinedUpdateFrequency)
    {
        settings.DefaultUpdateFrequency = configuration.GdsSettings.UpdateFrequency;
    }
    gdsFolder = configuration.GdsSettings.GdsCertificatesBaseFolder;

    return true;
}


[STAThread]
static void Init(object state)
{
    ConsoleClient.Client clienteOPC;
    clienteOPC = state as Client;
    clienteOPC.Init();
}

try
{
    // Add license.
    ApplicationLicenseManager.AddProcessLicenses(PlatformUtils.GetAssembly(typeof(Program)), "License.lic");

    // Create the certificate if it does not exist yet
    ApplicationInstanceBase.Default.AutoCreateCertificate = true;

    // Load the configuration
    var configuration = LoadInMemoryConfiguration();

    // Setting the SecurityProvider is required to be able to create certificates.
    //! [Set the security provider]
#if NETFRAMEWORK
                var securityProvider = new WindowsSecurityProvider();
#else
    var securityProvider = new BouncyCastleSecurityProvider();
#endif
    ApplicationInstanceBase.Default.SecurityProvider = securityProvider;
    //! [Set the security provider]

    // Load the client settings
    IClientConfiguration settings = Settings.LoadDefaults();

    Client client = new Client(settings);

#if NET45_OR_GREATER || NET || NETCOREAPP
    if (TryGetGdsSettings(settings, out UnifiedAutomation.UaSchema.GdsSettings gdsSettings, out string gdsFolder))
    {
        configuration.Set(gdsSettings);

        // Use other certificate and certificate stores if managed by GDS
        /// [GDS Security]
        ConfigurationInMemory clientConfigurationInMemory = configuration as ConfigurationInMemory;
        clientConfigurationInMemory.SetSecurity(PlatformUtils.CombinePath(PlatformUtils.IsWindows() ? "%CommonApplicationData%" : "%LocalApplicationData%", "UnifiedAutomation", "UaSdkNetBundleBinary", gdsFolder), "CN=ConsoleClient/O=UnifiedAutomation/DC=localhost");
        /// [GDS Security]

        // Set the class doing the GDS Pull Managent
        /// [GDS Pull]
        var pullManagement = new GdsPullManagement(ApplicationInstanceBase.Default);
        ApplicationInstanceBase.Default.GdsHandler = pullManagement;
        /// [GDS Pull]

        client.InitialGdsIteractionDone = new System.Threading.ManualResetEvent(false);
        pullManagement.IntitialGdsInteractionDone += (s, e) => { client.InitialGdsIteractionDone.Set(); };

        pullManagement.ApplicationRegistered += (s, e) =>
        {
            if (e.RegisteredApplicationMode == ApplicationRegisteredEventArgs.Mode.NewlyRegistered)
                Console.WriteLine("Application registered. Check if registration needs to be accepted in GDS config tool.");
        };
    }
#endif

    configuration.TraceSettings = TraceSettingsFromConfiguration(settings);
    ApplicationInstanceBase.Default.SetApplicationSettings(configuration);

    // Set application thread pool
    ApplicationInstanceBase.Default.ThreadPool = new ApplicationThreadPool(10, 1024);

    //! [Set UntrustedCertificate]
    ApplicationInstanceBase.Default.UntrustedCertificate += new UntrustedCertificateEventHandler(Application_UntrustedCertificate);
    //! [Set UntrustedCertificate]

    // start the application.
    ApplicationInstanceBase.Default.Start(Init, client);

}
catch (Exception e)
{
    Console.WriteLine($"Unexpected exception: {e.Message}");
}


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
