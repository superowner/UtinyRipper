using System.Collections.Generic;
using System.Linq;
using uTinyRipper.AssetExporters;
using uTinyRipper.Classes.ResourceManagers;
using uTinyRipper.Exporter.YAML;
using uTinyRipper.SerializedFiles;

namespace uTinyRipper.Classes
{
	public sealed class ResourceManager : GlobalGameManager
	{
		public ResourceManager(AssetInfo assetInfo) :
			base(assetInfo)
		{
		}

		/// <summary>
		/// 3.5.0 and greater and Release
		/// </summary>
		public static bool IsReadDependentAssets(Version version, TransferInstructionFlags flags)
		{
			return version.IsGreaterEqual(3, 5) && flags.IsRelease();
		}

		public override void Read(AssetReader reader)
		{
			base.Read(reader);

			m_container = reader.ReadStringTKVPArray<PPtr<Object>>();
			if (IsReadDependentAssets(reader.Version, reader.Flags))
			{
				m_dependentAssets = reader.ReadArray<ResourceManagerDependency>();
			}
		}

		public override IEnumerable<Object> FetchDependencies(ISerializedFile file, bool isLog = false)
		{
			foreach (Object asset in base.FetchDependencies(file, isLog))
			{
				yield return asset;
			}
			foreach (KeyValuePair<string, PPtr<Object>> asset in Container)
			{
				yield return asset.Value.FetchDependency(file, isLog, () => nameof(ResourceManager), ContainerName);
			}
			if (IsReadDependentAssets(file.Version, file.Flags))
			{
				foreach (ResourceManagerDependency dependentAsset in DependentAssets)
				{
					foreach (Object asset in dependentAsset.FetchDependencies(file, isLog))
					{
						yield return asset;
					}
				}
			}
		}

		protected override YAMLMappingNode ExportYAMLRoot(IExportContainer container)
		{
			YAMLMappingNode node = base.ExportYAMLRoot(container);
			node.Add(ContainerName, Container.ExportYAML(container));
			if (IsReadDependentAssets(container.Version, container.ExportFlags))
			{
				node.Add(DependentAssetsName, DependentAssets.ExportYAML(container));
			}
			return node;
		}

		public IReadOnlyList<KeyValuePair<string, PPtr<Object>>> Container => m_container;
		public ILookup<string, PPtr<Object>> ContainerMap => Container.ToLookup(t => t.Key, t => t.Value);
		public IReadOnlyList<ResourceManagerDependency> DependentAssets => m_dependentAssets;

		public const string ContainerName = "m_Container";
		public const string DependentAssetsName = "m_DependentAssets";

		private KeyValuePair<string, PPtr<Object>>[] m_container;
		private ResourceManagerDependency[] m_dependentAssets;
	}
}
