/**
* @license
* Visual Blocks Editor
*
* Copyright 2012 Google Inc.
* https://developers.google.com/blockly/
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

/**
* @fileoverview Utility functions for handling variables and procedure names.
* @author fraser@google.com (Neil Fraser)
*/
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Bridge;

namespace Blockly
{
	public class Names
	{
		string variablePrefix_;
		Dictionary<string, bool> reservedDict_;
		Dictionary<string, string> db_;
		Dictionary<string, bool> dbReverse_;

		/// <summary>
		/// Class for a database of entity names (variables, functions, etc).
		/// </summary>
		/// <param name="reservedWords">A comma-separated string of words that are
		/// illegal for use as names in a language (e.g. 'new,if,this,...').</param>
		/// <param name="opt_variablePrefix">Some languages need a '$' or a namespace
		/// before all variable names.</param>
		public Names(string reservedWords, string opt_variablePrefix = null)
		{
			this.variablePrefix_ = opt_variablePrefix == null ? "" : opt_variablePrefix;
			this.reservedDict_ = new Dictionary<string, bool>();
			if (reservedWords != null) {
				var splitWords = reservedWords.Split(',');
				for (var i = 0; i < splitWords.Length; i++) {
					this.reservedDict_[splitWords[i]] = true;
				}
			}
			this.reset();
		}

		/**
		 * When JavaScript (or most other languages) is generated, variable 'foo' and
		 * procedure 'foo' would collide.  However, Blockly has no such problems since
		 * variable get 'foo' and procedure call 'foo' are unambiguous.
		 * Therefore, Blockly keeps a separate type name to disambiguate.
		 * getName('foo', 'variable') -> 'foo'
		 * getName('foo', 'procedure') -> 'foo2'
		 */

		/// <summary>
		/// Empty the database and start from scratch.  The reserved words are kept.
		/// </summary>
		public void reset()
		{
			this.db_ = new Dictionary<string, string>();
			this.dbReverse_ = new Dictionary<string, bool>();
		}

		/// <summary>
		/// Convert a Blockly entity name to a legal exportable entity name.
		/// </summary>
		/// <param name="name">The Blockly entity name (no constraints).</param>
		/// <param name="type">The type of entity in Blockly
		/// ('VARIABLE', 'PROCEDURE', 'BUILTIN', etc...).</param>
		/// <returns>An entity name legal for the exported language.</returns>
		public string getName(string name, string type)
		{
			var normalized = name.ToLower() + '_' + type;
			var prefix = (type == Core.Variables.NAME_TYPE) ?
				this.variablePrefix_ : "";
			if (this.db_.ContainsKey(normalized)) {
				return prefix + this.db_[normalized];
			}
			var safeName = this.getDistinctName(name, type);
			this.db_[normalized] = safeName.Substring(prefix.Length);
			return safeName;
		}

		/// <summary>
		/// Convert a Blockly entity name to a legal exportable entity name.
		/// Ensure that this is a new name not overlapping any previously defined name.
		/// Also check against list of reserved words for the current language and
		/// ensure name doesn't collide.
		/// </summary>
		/// <param name="name">The Blockly entity name (no constraints).</param>
		/// <param name="type">The type of entity in Blockly
		/// ('VARIABLE', 'PROCEDURE', 'BUILTIN', etc...).</param>
		/// <returns>An entity name legal for the exported language.</returns>
		public string getDistinctName(string name, string type)
		{
			var safeName = this.safeName_(name);
			var i = 0;
			while (this.dbReverse_.ContainsKey(safeName + ((i == 0) ? "" : i.ToString())) ||
				   this.reservedDict_.ContainsKey(safeName + i.ToString())) {
				// Collision with existing name.  Create a unique name.
				i++;
			}
			if (i > 0)
				safeName += i;
			this.dbReverse_[safeName] = true;
			var prefix = (type == Core.Variables.NAME_TYPE) ?
				this.variablePrefix_ : "";
			return prefix + safeName;
		}

		/// <summary>
		/// Given a proposed entity name, generate a name that conforms to the
		/// [_A-Za-z][_A-Za-z0-9]* format that most languages consider legal for
		/// variables.
		/// </summary>
		/// <param name="name">Potentially illegal entity name.</param>
		/// <returns>Safe entity name.</returns>
		public string safeName_(string name)
		{
			if (name == null) {
				name = "unnamed";
			}
			else {
				// Unfortunately names in non-latin characters will look like
				// _E9_9F_B3_E4_B9_90 which is pretty meaningless.
				name = Script.EncodeURI(name.Replace(new Regex(@" "), "_").Replace(new Regex(@"[^\w]"), "_"));
				// Most languages don't allow names with leading numbers.
				if ("0123456789".IndexOf(name[0]) != -1) {
					name = "my_" + name;
				}
			}
			return name;
		}

		/// <summary>
		/// Do the given two entity names refer to the same entity?
		/// Blockly names are case-insensitive.
		/// </summary>
		/// <param name="name1">First name.</param>
		/// <param name="name2">Second name.</param>
		/// <returns>True if names are the same.</returns>
		public bool equals(string name1, string name2)
		{
			return name1.ToLower() == name2.ToLower();
		}
	}
}
