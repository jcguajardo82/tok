using System.Data;
using System.Data.SqlClient;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

using Soriana.PPS.Common.Data;
using Soriana.PPS.Common.Data.Dapper;
using Soriana.PPS.Common.Extensions;
using Soriana.PPS.DataAccess.Configuration;
using Soriana.PPS.DataAccess.Repository;
using Soriana.PPS.Card.SorianaApp.Services;
using Soriana.PPS.Common.DTO.Card;
using Soriana.PPS.DataAccess.Card;

[assembly: FunctionsStartup(typeof(Soriana.PPS.Card.SorianaApp.Startup))]
namespace Soriana.PPS.Card.SorianaApp
{
    public class Startup : FunctionsStartup
    {
        #region Constructors
        public Startup() { }
        #endregion

        #region Overrides Methods
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            base.ConfigureAppConfiguration(builder);
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {

            //Formatter Injection
            builder.Services.AddMvcCore().AddNewtonsoftJson(options => options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore);

            //Configuration Injection
            IConfiguration configuration = builder.GetContext().Configuration;
            builder.Services.Configure<IConfiguration>(configuration);

            //SeriLog Injection
            builder.Services.AddSeriLogConfiguration(configuration);

            //DataAccess Service Injection
            builder.Services.AddScoped<IDbConnection>(o =>
            {
                CardOptions cardOptions = new CardOptions();
                configuration.GetSection(CardOptions.CARDS_OPTIONS_CONFIGURATION).Bind(cardOptions);
                return new SqlConnection(cardOptions.ConnectionString);
            });
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(o =>
            {
                return new UnitOfWork(o.GetRequiredService<IDbConnection>());
            });

            //Business Service Injection
            builder.Services.AddScoped<ISorianaAppService, SorianaAppService>();
            builder.Services.AddScoped<IRepositoryCreate<AddCardRequest>, RepositoryWrite<AddCardRequest>>();
            builder.Services.AddScoped<ICardRepository, CardRepository>();
            builder.Services.AddScoped<ICardContext, CardContext>();
        }
        #endregion
    }
}
