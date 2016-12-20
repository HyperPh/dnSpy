﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files {
	sealed class HexBufferFileImpl : HexBufferFile {
		public override bool IsRemoved => isRemoved;
		public override event EventHandler Removed;
		public override bool IsStructuresInitialized => isStructuresInitialized;
		public override event EventHandler StructuresInitialized;

		readonly Lazy<StructureProviderFactory, VSUTIL.IOrderable>[] structureProviderFactories;
		StructureProvider[] structureProviders;
		bool isInitializing;
		bool isStructuresInitialized;
		bool isRemoved;

		public HexBufferFileImpl(Lazy<StructureProviderFactory, VSUTIL.IOrderable>[] structureProviderFactories, HexBuffer buffer, HexSpan span, string name, string filename, string[] tags)
			: base(buffer, span, name, filename, tags) {
			if (structureProviderFactories == null)
				throw new ArgumentNullException(nameof(structureProviderFactories));
			this.structureProviderFactories = structureProviderFactories;
		}

		void CreateStructureProviders(bool initialize) {
			if (structureProviders == null) {
				var list = new List<StructureProvider>(structureProviderFactories.Length);
				foreach (var lz in structureProviderFactories) {
					var provider = lz.Value.Create(this);
					if (provider != null)
						list.Add(provider);
				}
				structureProviders = list.ToArray();
			}
			if (initialize && !isStructuresInitialized && !isInitializing) {
				isInitializing = true;
				foreach (var provider in structureProviders)
					provider.Initialize();
				isStructuresInitialized = true;
				StructuresInitialized?.Invoke(this, EventArgs.Empty);
				isInitializing = false;
			}
		}

		public override ComplexData GetStructure(HexPosition position) {
			Debug.Assert(Span.Contains(position));
			CreateStructureProviders(true);
			foreach (var provider in structureProviders) {
				var structure = provider.GetStructure(position);
				if (structure != null)
					return structure;
			}
			return null;
		}

		public override ComplexData GetStructure(string id) {
			CreateStructureProviders(true);
			foreach (var provider in structureProviders) {
				var structure = provider.GetStructure(id);
				if (structure != null)
					return structure;
			}
			return null;
		}

		internal void RaiseRemoved() {
			if (isRemoved)
				throw new InvalidOperationException();
			isRemoved = true;
			Removed?.Invoke(this, EventArgs.Empty);
		}
	}
}
