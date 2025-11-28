using Chronicis.Api.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Chronicis.Api.Functions
{
    public class ArticleBaseClass
    {
        protected readonly JsonSerializerOptions _options;
        protected readonly ChronicisDbContext _context;

        public ArticleBaseClass(ChronicisDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _context = context;
        }
    }
}
