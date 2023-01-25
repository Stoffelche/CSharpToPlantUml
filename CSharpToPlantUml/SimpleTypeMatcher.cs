using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpToPlantUml {
	internal class SimpleTypeMatcher {
		ETypeMatching mTypeMatching;
		List<string> mPatterns;
		public SimpleTypeMatcher(ETypeMatching typeMatching, List<string> mPatterns) {
			mTypeMatching = typeMatching;
			this.mPatterns = mPatterns;
			if (mTypeMatching == ETypeMatching.Wildcard) {
				for (int i = 0; i < mPatterns.Count; i++) {
					mPatterns[i] = ConvertWildcardToRegEx(mPatterns[i]);
				}
			} else if (mTypeMatching == ETypeMatching.Regex) {
				for (int i = 0; i < mPatterns.Count; i++) {
					mPatterns[i] = MakeRegExExact(mPatterns[i]);
				}
			}
		}
		public bool Match(string typ) {
			foreach (string pattern in mPatterns) {
				if (mTypeMatching == ETypeMatching.Exact) {
					if (pattern == typ) return true;
				} else {
					if (Regex.IsMatch(typ, pattern))  return true;
				}
			}
			return false;
		}
		string MakeRegExExact(string pattern) {
			if (!pattern.StartsWith('^')) pattern = '^' + pattern;
			if (!pattern.EndsWith('$')) pattern = pattern + '$';
			return pattern;
		}
		string ConvertWildcardToRegEx(string pattern) {
			return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
		}
	}
}
