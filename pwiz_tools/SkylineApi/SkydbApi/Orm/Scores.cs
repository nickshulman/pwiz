﻿using NHibernate.Mapping.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.Orm
{
    [Class(Lazy = false, Table = nameof(Scores))]
    public class Scores : Entity
    {
    }
}
