global using App.Api;
global using App.Api.Models;
global using App.Api.Services;
global using Serilog;
global using System;
global using System.Security.Cryptography;
global using System.Security.Claims;
global using System.IdentityModel.Tokens.Jwt;
global using System.Text;
global using System.Text.Json;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.IdentityModel.Tokens;
global using Microsoft.OpenApi.Models;
global using Swashbuckle.AspNetCore.Filters;
global using Microsoft.AspNetCore.Authorization;