﻿using NHibernate.Mapping.Attributes;

namespace ResourcesOrganizer.DataModel
{
    [Class(Lazy = false)]
    public class LocalizedResource : Entity<LocalizedResource>
    {
        [Property]
        public long InvariantResourceId { get; set; }
        [Property]
        public string? Language { get; set; }
        /// <summary>
        /// Value found in the .resx that was read using "add" command
        /// </summary>
        [Property]
        public string? OriginalValue { get; set; }
        /// <summary>
        /// Value that was imported using "importtranslations" command
        /// </summary>
        [Property]
        public string? ReviewedValue { get; set; }
        [Property]
        public string? IssueType { get; set; }
        [Property]
        public string? ReviewedInvariantValue { get; set; }
    }
}
