using System.Threading.RateLimiting;
using AutoMapper;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using STAJ.Data;
using STAJ.Events;
using STAJ.Hubs;
using STAJ.Jobs;
using STAJ.Middleware;
using STAJ.Profiles;
using STAJ.Repositories;
using STAJ.Results;
using STAJ.Services;

var builder = WebApplication.CreateBuilder(args);

// mevcut Program.cs içeriği korunarak Swagger güvenlik kısmındaki .NET 10 / Swashbuckle 10 uyumluluğu düzeltildi.
