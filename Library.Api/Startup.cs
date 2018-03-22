using Library.Api.Entities;
using Library.Api.Helpers;
using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog.Extensions.Logging;
using System.Linq;

namespace Library.Api
{
    public class Startup
    {
        public const string VendorMediaType = "application/vnd.marvin.hateoas+json";

        private static IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container. For more information
        // on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
                setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());

                XmlDataContractSerializerInputFormatter xmlDataContractSerializerInputFormatter =
                    new XmlDataContractSerializerInputFormatter();

                xmlDataContractSerializerInputFormatter.SupportedMediaTypes.Add(
                    "application/vnd.marvin.authorwithdateofdeath.full+xml");

                setupAction.InputFormatters.Add(xmlDataContractSerializerInputFormatter);

                JsonOutputFormatter jsonOutputFormatter =
                    setupAction.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();

                jsonOutputFormatter?.SupportedMediaTypes.Add(VendorMediaType);

                JsonInputFormatter jsonInputFormatter =
                    setupAction.InputFormatters.OfType<JsonInputFormatter>().FirstOrDefault();

                jsonInputFormatter?.SupportedMediaTypes.Add("application/vnd.marvin.author.full+json");
                jsonInputFormatter?.SupportedMediaTypes.Add("application/vnd.marvin.authorwithdateofdeath.full+json");
            }).AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            string connectionString = _configuration["ConnectionStrings:LibraryDB"];
            services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

            services.AddScoped<ILibraryRepository, LibraryRepository>();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddScoped<IUrlHelper, UrlHelper>(factory =>
                new UrlHelper(factory.GetService<IActionContextAccessor>().ActionContext));

            services.AddTransient<IPropertyMappingService, PropertyMappingService>();

            services.AddTransient<ITypeHelperService, TypeHelperService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            LibraryContext libraryContext)
        {
            loggerFactory.AddNLog();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(subApp =>
                {
                    subApp.Run(async context =>
                    {
                        IExceptionHandlerFeature exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();

                        if (exceptionHandlerFeature != null)
                        {
                            ILogger logger = loggerFactory.CreateLogger("Global exception logger");

                            logger.LogError(500, exceptionHandlerFeature.Error, exceptionHandlerFeature.Error.Message);
                        }

                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected fault happened. Try again later.");
                    });
                });
            }

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            AutoMapper.Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Author, AuthorDto>()
                    .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                    .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.DateOfBirth.GetCurrentAge(src.DateOfDeath)));

                cfg.CreateMap<Book, BookDto>();

                cfg.CreateMap<AuthorForCreationDto, Author>();

                cfg.CreateMap<AuthorForCreationWithDateOfDeathDto, Author>();

                cfg.CreateMap<BookForCreationDto, Book>();

                cfg.CreateMap<BookForUpdateDto, Book>();

                cfg.CreateMap<Book, BookForUpdateDto>();
            });

            libraryContext.Database.Migrate();
            libraryContext.EnsureSeedDataForContext();

            app.UseMvc();
        }
    }
}