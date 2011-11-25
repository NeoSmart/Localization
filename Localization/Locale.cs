﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace NeoSmart.Localization
{
	public class Locale : IComparable<Locale>
	{
		private readonly Dictionary<string, StringCollection> _stringMap = new Dictionary<string, StringCollection>();

		public IEnumerable<StringCollection> StringCollections
		{
			get { return _stringMap.Values; }
			set
			{
				_stringMap.Clear();
				foreach (var stringCollection in value)
				{
					_stringMap.Add(stringCollection.Key, stringCollection);
				}
			}
		}

		public string Key { get; private set; }
		public string Name { get; internal set; }
		public bool RightToLeft { get; set; }
		public string ParentLocale { get; private set; }

		public int CompareTo(Locale other)
		{
			return Name.CompareTo(other.Name);
		}

		public Locale(string localeKey)
		{
			Key = localeKey;
			RightToLeft = false;
		}

		public string GetString(string collectionKey, string key)
		{
			return _stringMap[collectionKey].StringsTable[key];
		}

		private void LoadPropertiesXml(string xmlPath)
		{
			var xmlDocument = new XmlDocument();
			xmlDocument.Load(xmlPath);

			var node = xmlDocument.SelectSingleNode(@"/localization/locale");
			if (node == null)
				throw new IncompleteLocaleException("The required locale element 'locale' was not found.");

			node = xmlDocument.SelectSingleNode(@"/localization/locale/name");
			if (node == null)
				throw new IncompleteLocaleException("The required locale element 'name' was not found.");
			Name = node.InnerText;

			node = xmlDocument.SelectSingleNode(@"/localization/locale/rtl");
			RightToLeft = node != null && node.InnerText == "true";

			node = xmlDocument.SelectSingleNode(@"/localization/locale/parentLocale");
			ParentLocale = node != null ? node.InnerText : string.Empty;
		}

		private void SavePropertiesXml(string xmlPath)
		{
			var xmlDocument = new XmlDocument();

			var xmlDeclaration = xmlDocument.CreateXmlDeclaration(@"1.0", @"utf-8", null);

			xmlDocument.InsertBefore(xmlDeclaration, xmlDocument.DocumentElement);

			var xmlNode = xmlDocument.AppendChild(xmlDocument.CreateElement(@"localization"));
			xmlNode = xmlNode.AppendChild(xmlDocument.CreateElement(@"locale"));

			xmlNode.AppendChild(xmlDocument.CreateNode(XmlNodeType.Element, @"name", @"")).InnerText = Name;
			xmlNode.AppendChild(xmlDocument.CreateNode(XmlNodeType.Element, @"rtl", @"")).InnerText = RightToLeft.ToString().ToLower();
			xmlNode.AppendChild(xmlDocument.CreateNode(XmlNodeType.Element, @"parentLocale", @"")).InnerText = ParentLocale;

			xmlDocument.Save(xmlPath);
		}

		public bool Load(string xmlPath)
		{
			if (!File.Exists(xmlPath))
				return false;

			var folder = Path.GetDirectoryName(xmlPath);
			if (string.IsNullOrEmpty(folder))
				return false;

			LoadPropertiesXml(xmlPath);

			foreach (var stringFile in Directory.GetFiles(folder, @"*.xml"))
			{
				if (string.Compare(stringFile, xmlPath, true) == 0)
					continue;

				var stringKey = Path.GetFileNameWithoutExtension(stringFile);
				if (string.IsNullOrEmpty(stringKey))
					continue;

				var stringCollection = new StringCollection(stringKey);
				stringCollection.Load(stringFile);

				_stringMap[stringKey] = stringCollection;
			}

			return true;
		}

		public bool Save(string xmlPath, bool exportStrings = true)
		{
			xmlPath = Path.GetFullPath(xmlPath);
			SavePropertiesXml(xmlPath);

			if (exportStrings)
			{
				var folder = Path.GetDirectoryName(xmlPath);
				if (string.IsNullOrEmpty(folder))
					return false;

				foreach (var sCollection in _stringMap.Values)
				{
					sCollection.Save(Path.Combine(folder, sCollection.Key + @".xml"));
				}
			}

			return true;
		}
	}
}
