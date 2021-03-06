﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Outercurve Foundation">
//   Copyright (c) 2013, The Outercurve Foundation
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Chronozoom.Entities
{
    /// <summary>
    /// Storage implementation for ChronoZoom based on Entity Framework.
    /// </summary>
    public class Storage : DbContext
    {
        static Storage()
        {
            Trace = new TraceSource("Storage", SourceLevels.All);
        }

        public Storage()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<Storage, StorageMigrationsConfiguration>());
            Configuration.ProxyCreationEnabled = false;
        }

        public static TraceSource Trace { get; private set; }

        public DbSet<Timeline> Timelines { get; set; }

        public DbSet<Threshold> Thresholds { get; set; }

        public DbSet<Exhibit> Exhibits { get; set; }

        public DbSet<ContentItem> ContentItems { get; set; }

        public DbSet<Reference> References { get; set; }

        public DbSet<Tour> Tours { get; set; }

        public DbSet<Entities.Collection> Collections { get; set; }

        public DbSet<SuperCollection> SuperCollections { get; set; }

        public Collection<Timeline> TimelinesQuery(Guid collectionId, decimal startTime, decimal endTime, decimal span, Guid? commonAncestor, int maxElements)
        {
            Dictionary<Guid, Timeline> timelinesMap = new Dictionary<Guid, Timeline>();
            List<Timeline> timelines = FillTimelines(collectionId, timelinesMap, startTime, endTime, span, commonAncestor, maxElements);

            FillTimelineRelations(timelinesMap);

            return new Collection<Timeline>(timelines);
        }

        private void FillTimelineRelations(Dictionary<Guid, Timeline> timelinesMap)
        {
            if (!timelinesMap.Keys.Any())
                return;

            // Populate Exhibits
            string exhibitsQuery = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT *, Year as [Time] FROM Exhibits WHERE Timeline_Id IN ('{0}')",
                string.Join("', '", timelinesMap.Keys.ToArray()));

            var exhibitsRaw = Database.SqlQuery<ExhibitRaw>(exhibitsQuery);
            Dictionary<Guid, Exhibit> exhibits = new Dictionary<Guid, Exhibit>();
            foreach (ExhibitRaw exhibitRaw in exhibitsRaw)
            {
                if (exhibitRaw.ContentItems == null)
                    exhibitRaw.ContentItems = new System.Collections.ObjectModel.Collection<ContentItem>();

                if (exhibitRaw.References == null)
                    exhibitRaw.References = new System.Collections.ObjectModel.Collection<Reference>();

                if (timelinesMap.Keys.Contains(exhibitRaw.Timeline_ID))
                {
                    timelinesMap[exhibitRaw.Timeline_ID].Exhibits.Add(exhibitRaw);
                    exhibits[exhibitRaw.Id] = exhibitRaw;
                }
            }

            if (exhibits.Keys.Any())
            {
                // Populate Content Items
                string contentItemsQuery = string.Format(
                    CultureInfo.InvariantCulture,
                    "SELECT * FROM ContentItems WHERE Exhibit_Id IN ('{0}')",
                    string.Join("', '", exhibits.Keys.ToArray()));
                var contentItemsRaw = Database.SqlQuery<ContentItemRaw>(contentItemsQuery);
                foreach (ContentItemRaw contentItemRaw in contentItemsRaw)
                {
                    if (exhibits.Keys.Contains(contentItemRaw.Exhibit_ID))
                    {
                        exhibits[contentItemRaw.Exhibit_ID].ContentItems.Add(contentItemRaw);
                    }
                }

                // Populate References
                string referencesQuery = string.Format(CultureInfo.InvariantCulture,
                    "SELECT * FROM [References] WHERE Exhibit_Id IN ('{0}')",
                    string.Join("', '", exhibits.Keys.ToArray()));
                var referencesRaw = Database.SqlQuery<ReferenceRaw>(referencesQuery);
                foreach (ReferenceRaw referenceRaw in referencesRaw)
                {
                    if (exhibits.Keys.Contains(referenceRaw.Exhibit_ID))
                    {
                        exhibits[referenceRaw.Exhibit_ID].References.Add(referenceRaw);
                    }
                }
            }
        }

        private List<Timeline> FillTimelines(Guid collectionId, Dictionary<Guid, Timeline> timelinesMap, decimal startTime, decimal endTime, decimal span, Guid? commonAncestor, int maxElements)
        {
            List<Timeline> timelines = new List<Timeline>();
            Dictionary<Guid, Guid?> timelinesParents = new Dictionary<Guid, Guid?>();

            // Populate References
            string timelinesQuery = "SELECT TOP({0}) *, FromYear as [Start], ToYear as [End] FROM Timelines WHERE FromYear >= {1} AND ToYear <= {2} AND ToYear-FromYear >= {3} AND Collection_Id = {4} OR Id = {5} ORDER BY ToYear-FromYear DESC";
            var timelinesRaw = Database.SqlQuery<TimelineRaw>(timelinesQuery, maxElements, startTime, endTime, span, collectionId, commonAncestor);

            foreach (TimelineRaw timelineRaw in timelinesRaw)
            {
                if (timelineRaw.Exhibits == null)
                    timelineRaw.Exhibits = new System.Collections.ObjectModel.Collection<Exhibit>();

                timelinesParents[timelineRaw.Id] = timelineRaw.Timeline_ID;
                timelinesMap[timelineRaw.Id] = timelineRaw;
            }

            // Build the timelines tree by assigning each timeline to its parent
            foreach (Timeline timeline in timelinesMap.Values)
            {
                Guid? parentId = timelinesParents[timeline.Id];

                // If the timeline should be a root timeline
                if (timeline.Id == commonAncestor || parentId == null || !timelinesMap.Keys.Contains((Guid)parentId))
                {
                    timelines.Add(timeline);
                }
                else
                {
                    Timeline parentTimeline = timelinesMap[(Guid)parentId];
                    if (parentTimeline.ChildTimelines == null)
                        parentTimeline.ChildTimelines = new System.Collections.ObjectModel.Collection<Timeline>();

                    parentTimeline.ChildTimelines.Add(timeline);
                }
            }

            return timelines;
        }

        // Recursively deletes every child timeline and exhibit (with references and content items) form timeline with given guid. 
        public void DeleteTimeline(Guid id)
        {
            var timelineIDs = GetChildTimelinesIds(id); // list of ids of child timelines
            var exhibitIDs = GetChildExhibitsIds(id); // list of ids of exhibits

            // recursively delete timelines
            while (timelineIDs.Count != 0)
            {
                DeleteTimeline(timelineIDs.First());
                timelineIDs.RemoveAt(0);
            }

            // recursively delete exhibits
            while (exhibitIDs.Count != 0)
            {
                DeleteExhibit(exhibitIDs.First());
                exhibitIDs.RemoveAt(0);
            }

            Timeline removeTimeline = this.Timelines.Find(id);
            this.Timelines.Remove(removeTimeline);
        }

        // Deletes every content item and reference from exhibit with given guid.
        public void DeleteExhibit(Guid id)
        {
            var exhibitsIDs = GetChildContentItemsIds(id); // list of ids of content items
            var referencesIDs = GetChildReferencesIds(id); // list of ids of references

            // delete references
            while (referencesIDs.Count != 0)
            {
                var r = this.References.Find(referencesIDs.First());
                this.References.Remove(r);
                referencesIDs.RemoveAt(0);
            }

            // delete content items
            while (exhibitsIDs.Count != 0)
            {
                var e = this.ContentItems.Find(exhibitsIDs.First());
                this.ContentItems.Remove(e);
                exhibitsIDs.RemoveAt(0);
            }

            Exhibit deleteExhibit = this.Exhibits.Find(id);
            this.Exhibits.Remove(deleteExhibit);
        }

        // Returns list of ids of chilt timelines of timeline with given id.
        private List<Guid> GetChildTimelinesIds(Guid id)
        {
            var timelines = new List<Guid>();

            string timelinesQuery = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT * FROM Timelines WHERE Timeline_Id IN ('{0}')",
                string.Join("', '", id));
            var timelinesRaw = Database.SqlQuery<TimelineRaw>(timelinesQuery);
           
            foreach (TimelineRaw timelineRaw in timelinesRaw)
                timelines.Add(timelineRaw.Id);

            return timelines;
        }

        // Returns list of ids of child exhibits of timeline with given id.
        private List<Guid> GetChildExhibitsIds(Guid id)
        {
            var exhibits = new List<Guid>();

            string exhibitsQuery = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT *, Year as [Time] FROM Exhibits WHERE Timeline_Id IN ('{0}')",
                string.Join("', '", id));
            var exhibitsRaw = Database.SqlQuery<ExhibitRaw>(exhibitsQuery);

            foreach (ExhibitRaw exhibitRaw in exhibitsRaw)
                exhibits.Add(exhibitRaw.Id);

            return exhibits;
        }

        // Returns list of ids of child content items of exhibit with given id.
        private List<Guid> GetChildContentItemsIds(Guid id)
        {
            var contentItems = new List<Guid>();

            // Find child content items
            string contentItemsQuery = string.Format(
                    CultureInfo.InvariantCulture,
                    "SELECT * FROM ContentItems WHERE Exhibit_Id IN ('{0}')",
                    string.Join("', '", id));
            var contentItemsRaw = Database.SqlQuery<ContentItemRaw>(contentItemsQuery);

            foreach (ContentItemRaw contentItemRaw in contentItemsRaw)
                contentItems.Add(contentItemRaw.Id);      
            return contentItems;
        }

        // Returns list of ids of child references of exhibit with given id.
        private List<Guid> GetChildReferencesIds(Guid id)
        {
            var references = new List<Guid>();

            // Find exhibit's references
            string referencesQuery = string.Format(CultureInfo.InvariantCulture,
                "SELECT * FROM [References] WHERE Exhibit_Id IN ('{0}')",
                string.Join("', '", id));
            var referencesRaw = Database.SqlQuery<ReferenceRaw>(referencesQuery);

            foreach (ReferenceRaw referenceRaw in referencesRaw)
                references.Add(referenceRaw.Id);

            return references;
        }
    }
}
