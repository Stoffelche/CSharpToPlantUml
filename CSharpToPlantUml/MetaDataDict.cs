using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace CSharpToPlantUml {
	public class MetaDataDict : IXmlSerializable , IEnumerable<KeyValuePair<string, object>>{
		Dictionary<string, object> mDict = new Dictionary<string, object>();
		public XmlSchema? GetSchema() {
			return null;
		}
		public void ReadXml(XmlReader reader) {
			string s = reader.ReadInnerXml().Trim();
			if (s.Length > 0) {
				reader = XmlReader.Create(new StringReader(string.Format("<root>{0}</root>", s)));
				reader.Read();
				DoRead(reader, true);
			}
		}
		public object this[string idx] {
			get { if (mDict.TryGetValue(idx, out var value)) return value; else return null; }
			set {
				if (value is string || value is MetaDataDict) mDict[idx] = value;
				else throw new ArgumentException("only string or MetaDataDict as type is allowed");
			}
		}
		void DoRead(XmlReader reader, bool readNext) {
			int depth = reader.Depth;
			if (!readNext) depth -= 1;
			while (true) {
				if (readNext) if (!reader.Read()) break;
				readNext = true;
				if (reader.NodeType == System.Xml.XmlNodeType.EndElement) {
					if (reader.Depth == depth) break;
					else continue;
				}
				string key = reader.Name;
				object value = ReadValue(reader);
				mDict[key] = value;
			}
		}
		object ReadValue(XmlReader reader) {
			reader.Read();
			if (reader.NodeType == XmlNodeType.Text) return reader.Value;
			MetaDataDict dict = new MetaDataDict();
			dict.DoRead(reader, false);
			return dict;
		}
		public void WriteXml(XmlWriter writer) {
			foreach (var item in mDict) {
				writer.WriteStartElement(item.Key);
				if (item.Value is string s) {
					writer.WriteValue(s);
				} else {
					MetaDataDict dict = (MetaDataDict)item.Value;
					dict.WriteXml(writer);
				}
				writer.WriteEndElement();
			}
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			return mDict.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return mDict.GetEnumerator();
		}

		public static MetaDataDict operator +(MetaDataDict m1, (string k, object v) kv) {
			m1[kv.k] = kv.v;
			return m1;
		}
	}
}
