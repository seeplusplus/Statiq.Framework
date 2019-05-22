﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using Wyam.Common.Content;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Common.IO;
using Wyam.Common.Tracing;
using Wyam.Core.Meta;
using Wyam.Core.Modules;

namespace Wyam.Core.Documents
{
    internal class DocumentFactory : IDocumentFactory
    {
        // Keep track of document versions in the factory since we might create more than one clone from the same source document
        private readonly ConcurrentDictionary<string, int> _versions = new ConcurrentDictionary<string, int>();

        private readonly MetadataDictionary _settings;

        public DocumentFactory(MetadataDictionary settings)
        {
            _settings = settings;
        }

        public IDocument GetDocument(
            IExecutionContext context,
            IDocument originalDocument,
            FilePath source,
            FilePath destination,
            IEnumerable<KeyValuePair<string, object>> metadata,
            IContentProvider contentProvider)
        {
            Document newDocument = null;
            if (originalDocument == null || ModuleExtensions.AsNewDocumentModules.Contains(context.Module))
            {
                newDocument = new Document(_settings, source, destination, contentProvider, metadata);
            }

            // Get the next version
            int version = _versions.AddOrUpdate(
                newDocument?.Id ?? originalDocument.Id,
                newDocument?.Version ?? originalDocument.Version + 1,
                (_, ver) => ver + 1);

            newDocument = newDocument ?? new Document((Document)originalDocument, version, source, destination, contentProvider, metadata);

            Trace.Verbose($"Created document with ID {newDocument.Id}.{newDocument.Version}{(originalDocument == null ? string.Empty : " from version " + originalDocument.Version)} and source {newDocument.SourceString()}");

            return newDocument;
        }
    }
}
