﻿using Nop.Web.Framework.Models;

namespace Nop.Plugin.Feed.GoogleShopping.Models
{
    public record GeneratedFileModel : BaseNopModel
    {
        public string StoreName { get; set; }
        public string FileUrl { get; set; }
    }
}
