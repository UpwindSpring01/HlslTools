﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using ShaderTools.CodeAnalysis.Editor.Commands;
using ShaderTools.CodeAnalysis.Editor.Shared.Extensions;
using ShaderTools.CodeAnalysis.Editor.Shared.Tagging;
using ShaderTools.CodeAnalysis.Text.Shared.Extensions;
using ShaderTools.Utilities.Diagnostics;

namespace ShaderTools.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.NavigateToHighlightedReference, ContentTypeNames.ShaderToolsContentType)]
    internal partial class NavigateToHighlightedReferenceCommandHandler :
        ICommandHandler<NavigateToHighlightedReferenceCommandArgs>
    {
        private readonly IOutliningManagerService _outliningManagerService;
        private readonly IViewTagAggregatorFactoryService _tagAggregatorFactory;

        [ImportingConstructor]
        public NavigateToHighlightedReferenceCommandHandler(
            IOutliningManagerService outliningManagerService,
            IViewTagAggregatorFactoryService tagAggregatorFactory)
        {
            _outliningManagerService = outliningManagerService ?? throw new ArgumentNullException(nameof(outliningManagerService));
            _tagAggregatorFactory = tagAggregatorFactory ?? throw new ArgumentNullException(nameof(tagAggregatorFactory));
        }

        public CommandState GetCommandState(NavigateToHighlightedReferenceCommandArgs args, Func<CommandState> nextHandler)
        {
            using (var tagAggregator = _tagAggregatorFactory.CreateTagAggregator<NavigableHighlightTag>(args.TextView))
            {
                var tagUnderCursor = FindTagUnderCaret(tagAggregator, args.TextView);
                return tagUnderCursor == null ? CommandState.Unavailable : CommandState.Available;
            }
        }

        public void ExecuteCommand(NavigateToHighlightedReferenceCommandArgs args, Action nextHandler)
        {
            using (var tagAggregator = _tagAggregatorFactory.CreateTagAggregator<NavigableHighlightTag>(args.TextView))
            {
                var tagUnderCursor = FindTagUnderCaret(tagAggregator, args.TextView);

                if (tagUnderCursor == null)
                {
                    nextHandler();
                    return;
                }

                var spans = GetTags(tagAggregator, args.TextView.TextSnapshot.GetFullSpan()).ToList();

                Contract.ThrowIfFalse(spans.Any(), "We should have at least found the tag under the cursor!");

                var destTag = GetDestinationTag(tagUnderCursor.Value, spans, args.Direction);

                if (args.TextView.TryMoveCaretToAndEnsureVisible(destTag.Start, _outliningManagerService))
                {
                    args.TextView.SetSelection(destTag);
                }
            }
        }

        private static IEnumerable<SnapshotSpan> GetTags(
            ITagAggregator<NavigableHighlightTag> tagAggregator,
            SnapshotSpan span)
        {
            return tagAggregator.GetTags(span)
                                .SelectMany(tag => tag.Span.GetSpans(span.Snapshot.TextBuffer))
                                .OrderBy(tag => tag.Start);
        }

        private static SnapshotSpan GetDestinationTag(
            SnapshotSpan tagUnderCursor,
            List<SnapshotSpan> orderedTagSpans,
            NavigateDirection direction)
        {
            var destIndex = orderedTagSpans.BinarySearch(tagUnderCursor, new StartComparer());

            Contract.ThrowIfFalse(destIndex >= 0, "Expected to find start tag in the collection");

            destIndex += direction == NavigateDirection.Down ? 1 : -1;
            if (destIndex < 0)
            {
                destIndex = orderedTagSpans.Count - 1;
            }
            else if (destIndex == orderedTagSpans.Count)
            {
                destIndex = 0;
            }

            return orderedTagSpans[destIndex];
        }

        private SnapshotSpan? FindTagUnderCaret(
            ITagAggregator<NavigableHighlightTag> tagAggregator,
            ITextView textView)
        {
            // We always want to be working with the surface buffer here, so this line is correct
            var caretPosition = textView.Caret.Position.BufferPosition.Position;

            var tags = GetTags(tagAggregator, new SnapshotSpan(textView.TextSnapshot, new Span(caretPosition, 0)));
            return tags.Any()
                ? tags.First()
                : (SnapshotSpan?) null;
        }
    }
}
