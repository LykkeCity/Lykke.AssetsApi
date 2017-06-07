﻿using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.MarketProfileService.Api.Models;
using Lykke.MarketProfileService.Core;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;

namespace Lykke.MarketProfileService.Api.Middleware
{
    public class GlobalErrorHandlerMiddleware
    {
        private readonly ILog _log;
        private readonly RequestDelegate _next;

        public GlobalErrorHandlerMiddleware(RequestDelegate next, ILog log)
        {
            _log = log;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                await LogError(context, ex);

                await SendError(context);
            }
        }

        private async Task LogError(HttpContext context, Exception ex)
        {
            using (var ms = new MemoryStream())
            {
                context.Request.Body.CopyTo(ms);

                ms.Seek(0, SeekOrigin.Begin);

                await _log.LogPartFromStream(ms, Constants.ComponentName, context.Request.GetUri().AbsoluteUri, ex);
            }
        }

        private static async Task SendError(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = 500;

            var response = new ApiError
            {
                Code = ErrorCodes.RuntimeProblem,
                Msg = "Technical problems"
            };

            await ctx.Response.WriteAsync(response.ToJson());
        }
    }
}