using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Chronicis.Api.Functions
{
    public class ArticleBaseClass : BaseAuthenticatedFunction
    {
        protected readonly JsonSerializerOptions _options;
        protected readonly ChronicisDbContext _context;

        public ArticleBaseClass(ChronicisDbContext context,
            IUserService userService,
            ILogger logger,
            IOptions<Auth0Configuration> auth0Config) : base(userService, auth0Config, logger)
        {
            ArgumentNullException.ThrowIfNull(context);

            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _context = context;
        }
    }
}
