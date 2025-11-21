using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Chronicis.Api.Functions
{
    public class ArticleBaseClass
    {
        protected readonly JsonSerializerOptions _options;

        public ArticleBaseClass()
        {
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }
    }
}
