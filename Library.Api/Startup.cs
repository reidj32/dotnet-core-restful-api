using AspNetCoreRateLimit;
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
using System;
using System.Collections.Generic;
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

        // ReSharper disable once UnusedMember.Global
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(SetupMvcOptions).AddJsonOptions(SetupMvcJsonOption);
            services.AddHttpCacheHeaders(SetExpireCacheHeaders, SetupValidationCacheHeaders);
            services.AddResponseCaching();
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(SetupIpRateLimitOptions);

            services.AddDbContext<LibraryContext>(SetupDbContext);
            services.AddScoped<ILibraryRepository, LibraryRepository>();
            services.AddScoped<IUrlHelper, UrlHelper>(SetupUrlHelper);
            services.AddTransient<IPropertyMappingService, PropertyMappingService>();
            services.AddTransient<ITypeHelperService, TypeHelperService>();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        }

        // ReSharper disable once UnusedMember.Global
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            LibraryContext libraryContext)
        {
            loggerFactory.AddNLog();

            ConfigureExceptionHandling(app, env, loggerFactory);
            ConfigureObjectMapping();

            libraryContext.Database.Migrate();
            libraryContext.EnsureSeedDataForContext();

            app.UseIpRateLimiting();
            app.UseResponseCaching();
            app.UseHttpCacheHeaders();
            app.UseMvc();
        }

        private static void SetupMvcOptions(MvcOptions setupAction)
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
        }

        private static void SetupMvcJsonOption(MvcJsonOptions options)
        {
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            JsonConvert.DefaultSettings = () => options.SerializerSettings;
        }

        private static UrlHelper SetupUrlHelper(IServiceProvider factory)
        {
            return new UrlHelper(factory.GetService<IActionContextAccessor>().ActionContext);
        }

        private static void SetupDbContext(DbContextOptionsBuilder o)
        {
            string connectionString = _configuration["ConnectionStrings:LibraryDB"];
            o.UseSqlServer(connectionString);
        }

        private static void SetupIpRateLimitOptions(IpRateLimitOptions options)
        {
            options.GeneralRules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Endpoint = "*",
                    Limit = 1000,
                    Period = "5m"
                },
                new RateLimitRule
                {
                    Endpoint = "*",
                    Limit = 200,
                    Period = "10s"
                }
            };
        }

        private static void SetupValidationCacheHeaders(Marvin.Cache.Headers.ValidationModelOptions validationModelOptions)
        {
            validationModelOptions.AddMustRevalidate = true;
        }

        private static void SetExpireCacheHeaders(Marvin.Cache.Headers.ExpirationModelOptions expirationModelOptions)
        {
            expirationModelOptions.MaxAge = 600;
        }

        private static void ConfigureObjectMapping()
        {
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
        }

        private static void ConfigureExceptionHandling(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
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
        }
    }
}