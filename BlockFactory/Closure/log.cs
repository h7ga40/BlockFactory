// Copyright 2009 The Closure Library Authors. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/// <summary>
/// Google's common JavaScript library
/// https://developers.google.com/closure/library/
/// </summary>
namespace goog
{
	public class log
	{
		public static bool ENABLED = false;

		public class Logger
		{
			private string name;
			private int level;

			public Logger(string name)
			{
				this.name = name;
			}

			internal void setLevel(int value)
			{
				this.level = value;
			}

			internal void warning(string msg)
			{
			}

			internal void fine(string msg)
			{
			}
		}

		/// <summary>
		/// Finds or creates a logger for a named subsystem. If a logger has already been
		/// created with the given name it is returned. Otherwise a new logger is
		/// created. If a new logger is created its log level will be configured based
		/// on the goog.debug.LogManager configuration and it will configured to also
		/// send logging output to its parent's handlers.
		/// @see goog.debug.LogManager
		/// </summary>
		/// <param name="name">A name for the logger. This should be a dot-separated
		/// name and should normally be based on the package name or class name of
		/// the subsystem, such as goog.net.BrowserChannel.</param>
		/// <param name="opt_level">If provided, override the
		/// default logging level with the provided level.</param>
		/// <returns>The named logger or null if logging is disabled.</returns>
		internal static Logger getLogger(string name, int? opt_level = null)
		{
			if (ENABLED) {
				var logger = new Logger(name);
				if (opt_level != null) {
					logger.setLevel(opt_level.Value);
				}
				return logger;
			}
			else {
				return null;
			}
		}

		/// <summary>
		/// Logs a message at the Level.WARNING level.
		/// If the logger is currently enabled for the given message level then the
		/// given message is forwarded to all the registered output Handler objects.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="msg">The message to log.</param>
		internal static void warning(Logger logger, string msg)
		{
			if (ENABLED && logger != null) {
				logger.warning(msg);
			}
		}

		/// <summary>
		/// Logs a message at the Level.Fine level.
		/// If the logger is currently enabled for the given message level then the
		/// given message is forwarded to all the registered output Handler objects.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="msg">The message to log.</param>
		internal static void fine(Logger logger, string msg)
		{
			if (ENABLED && logger != null) {
				logger.fine(msg);
			}
		}
	}
}
