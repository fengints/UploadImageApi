﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UploadImage.Models
{
    public class FileInformation
    {
        public virtual string fileName { get; set; }
        public virtual byte[] data { get; set; }
    }
}
