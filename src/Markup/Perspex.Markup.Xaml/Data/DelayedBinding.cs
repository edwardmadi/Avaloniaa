﻿// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Perspex.Controls;
using Perspex.Data;

namespace Perspex.Markup.Xaml.Data
{
    /// <summary>
    /// Provides delayed bindings for controls.
    /// </summary>
    /// <remarks>
    /// The XAML engine applies its bindings in a delayed manner where bindings are only applied
    /// when a control is added to the visual tree. This was done because applying bindings as soon
    /// as controls are created means that long-form bindings (i.e. bindings that don't use the
    /// `{Binding}` markup extension) don't work as the binding is applied to the property before
    /// the binding properties are set, and looking at WPF it uses a similar mechanism for bindings
    /// that come from XAML.
    /// </remarks>
    public static class DelayedBinding
    {
        private static ConditionalWeakTable<IControl, List<Entry>> _entries = 
            new ConditionalWeakTable<IControl, List<Entry>>();

        /// <summary>
        /// Adds a delayed binding to a control.
        /// </summary>
        /// <param name="target">The control.</param>
        /// <param name="property">The property on the control to bind to.</param>
        /// <param name="binding">The binding.</param>
        public static void Add(IControl target, PerspexProperty property, IBinding binding)
        {
            if (target.IsAttachedToVisualTree)
            {
                target.Bind(property, binding);
            }
            else
            {
                List<Entry> bindings;

                if (!_entries.TryGetValue(target, out bindings))
                {
                    bindings = new List<Entry>();
                    _entries.Add(target, bindings);

                    // TODO: Make this a weak event listener.
                    target.AttachedToVisualTree += ApplyBindings;
                }

                bindings.Add(new Entry(binding, property));
            }
        }

        /// <summary>
        /// Applies any delayed bindings to a control.
        /// </summary>
        /// <param name="control">The control.</param>
        public static void ApplyBindings(IControl control)
        {
            List<Entry> bindings;

            if (_entries.TryGetValue(control, out bindings))
            {
                foreach (var binding in bindings)
                {
                    control.Bind(binding.Property, binding.Binding);
                }

                _entries.Remove(control);
            }
        }

        private static void ApplyBindings(object sender, VisualTreeAttachmentEventArgs e)
        {
            var target = (IControl)sender;
            ApplyBindings(target);
            target.AttachedToVisualTree -= ApplyBindings;
        }

        private class Entry
        {
            public Entry(IBinding binding, PerspexProperty property)
            {
                Binding = binding;
                Property = property;
            }

            public IBinding Binding { get; }
            public PerspexProperty Property { get; }
        }
    }
}
