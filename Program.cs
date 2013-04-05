/** 
 * Copyright 2013 Pavel Puchkarev
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
 **/

// Code is written to be read from top to bottom.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

public static class Program {
	// STA required for clipboard management
	[STAThread]
	public static void Main(string[] args) {
		try {
			Operation operation = GetOperation(args);
			string service = GetService(args);
			string credentials = GetCredentials();

			List<Authorization> authorizations = LoadAuthorizations(credentials: credentials);
			switch (operation) {
				case Operation.Get: {
						Authorization auth = Authorization.FindByService(authorizations: authorizations, service: service);
						CheckApplications();
						ExposeAuthorizationInfo(authorization: auth);
						break;
					}
				case Operation.Add: {
						Authorization auth = GetAuthorizationInfo(service: service);
						authorizations.Add(item: auth);
						SaveAuthorizations(authorizations: authorizations, credentials: credentials);
						break;
					}
				case Operation.Remove: {
						Authorization auth = Authorization.FindByService(authorizations: authorizations, service: service);
						authorizations.Remove(item: auth);
						SaveAuthorizations(authorizations: authorizations, credentials: credentials);
						break;
					}
				default: {
						throw new InvalidOperationException(string.Format("Unknown operation given : {0}", operation.ToString()));
					}
			}
		} catch (Exception ex) {
			Console.Error.WriteLine(ex.Message);
			Environment.ExitCode = -1;
		}
		Console.WriteLine("Press enter to quit ...");
		WaitForEnterKey();
	}

	#region Inner Classes

	public enum Operation {
		Get = 0,
		Add,
		Remove,
	}

	public class Authorization {
		public Authorization(string service, string username, string password) {
			this.Service = service;
			this.Username = username;
			this.Password = password;
		}

		public readonly string Service;
		public readonly string Username;
		public readonly string Password;

		public static Authorization FindByService(IEnumerable<Authorization> authorizations, string service) {
			return new List<Authorization>(authorizations).Find(new Predicate<Authorization>(delegate(Authorization auth) { return auth.Service == service; }));
		}
	}

	#endregion Inner Classes

	#region Argument Processing

	public static void ShowHelp() {
		Console.WriteLine("Usage: add|remove|get service");
	}

	public static Operation GetOperation(string[] args) {
		if (args.Length < 1) { ShowHelp(); }
		return (Operation)Enum.Parse(typeof(Operation), args[0], true);
	}
	public static string GetService(string[] args) {
		if (args.Length < 2) { ShowHelp(); }
		return args[1];
	}

	#endregion Argument Processing

	#region User Interactions

	public static void WaitForEnterKey() {
		while (Console.ReadKey(intercept: true).Key != ConsoleKey.Enter) ;
	}

	public static string GetInputWithoutEcho() {
		ConsoleKeyInfo current;
		List<char> input = new List<char>();
		while ((current = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter) { input.Add(current.KeyChar); }
		Console.WriteLine();
		return new string(input.ToArray());
	}

	public static string GetCredentials() {
		Console.Write("Enter main password : ");
		return GetInputWithoutEcho();
	}

	public static Authorization GetAuthorizationInfo(string service) {
		Console.WriteLine("Please enter authorization information for {0} service.", service);
		Console.Write("User name : ");
		string username = Console.ReadLine();
		Console.Write("Password : ");
		string password = GetInputWithoutEcho();
		return new Authorization(service: service, username: username, password: password);
	}

	public static void ExposeAuthorizationInfo(Authorization authorization) {
		Console.WriteLine("Exposing authorization information for {0} service.", authorization.Service);
		if (authorization.Username != string.Empty) {
			Console.WriteLine("User name is on clipboard ... press enter for password.");
			SetClipboardContent(content: authorization.Username);
			WaitForEnterKey();
		}
		Console.WriteLine("Password is on clipboard ... press enter to clear.");
		SetClipboardContent(content: authorization.Password);
		WaitForEnterKey();
		ClearClipboardContent();
	}

	#endregion User Interactions

	#region Operating System Interactions

	public static void ClearClipboardContent() {
		switch(Environment.OSVersion.Platform) {
			case PlatformID.Win32NT:
				Clipboard.Clear();
				break;
			case PlatformID.Unix:
				SetClipboardContent(string.Empty);
				break;
		}
	}

	public static void SetClipboardContent(string content) {
		switch(Environment.OSVersion.Platform) {
			case PlatformID.Win32NT:
				Clipboard.SetText(content);
				break;
			case PlatformID.Unix:
				Process p = new Process();
				p.StartInfo.FileName = "xclip";
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardInput = true;
				p.Start();
				p.StandardInput.WriteLine(content);
				p.StandardInput.Dispose();
				p.WaitForExit();
				break;
		}
	}

	public static void CheckApplications() {
		HashSet<string> suspectApplications = new HashSet<string> {
			"ditto", "clipx", "clcl", "arsclip", "clipmate",
		};

		List<Process> suspect = Process
			.GetProcesses()
			.Where((process) => suspectApplications.Any((partialName) => process.ProcessName.Contains(partialName)))
			.ToList();

		suspect.ForEach((process) => Console.WriteLine("Found suspect application : {0}", process.ProcessName));
		if (suspect.Count > 0) {
			Console.WriteLine("Press enter to continue.");
			WaitForEnterKey();
		}
	}

	#endregion Operating System Interactions

	#region Data Management

	/*
	 * Authorization file info structure (Xml):
	 * <root>
	 *     <info service="name" username="username" password="password"/>
	 *     <info ...
	 *     ...
	 * </root>
	 * 
	 * */

	public const string AuthorizationInfoFilePath = "passman.data";

	public const string Root_ElementName = "root";
	public const string Entry_ElementName = "info";
	public const string Service_AttributeName = "service";
	public const string Username_AttributeName = "username";
	public const string Password_AttributeName = "password";

	public static List<Authorization> LoadAuthorizations(string credentials) {
		List<Authorization> authorizations = new List<Authorization>();
		if (!File.Exists(AuthorizationInfoFilePath)) { return authorizations; }

		string decryptedContent = Decrypt(File.ReadAllBytes(AuthorizationInfoFilePath), credentials);
		XmlDocument structuredContent = new XmlDocument();
		structuredContent.LoadXml(decryptedContent);

		foreach (XmlNode authorizationEntry in structuredContent.SelectNodes(string.Format("/{0}/{1}", Root_ElementName, Entry_ElementName))) {
			authorizations.Add(new Authorization(
				service: authorizationEntry.Attributes[Service_AttributeName].Value,
				username: authorizationEntry.Attributes[Username_AttributeName].Value,
				password: authorizationEntry.Attributes[Password_AttributeName].Value));
		}

		return authorizations;
	}

	public static void SaveAuthorizations(List<Authorization> authorizations, string credentials) {
		StringBuilder decryptedContent = new StringBuilder();
		using (XmlWriter writer = XmlWriter.Create(decryptedContent)) {
			writer.WriteStartDocument();
			writer.WriteStartElement(Root_ElementName);

			foreach (Authorization authorization in authorizations) {
				writer.WriteStartElement(Entry_ElementName);

				writer.WriteAttributeString(Service_AttributeName, authorization.Service);
				writer.WriteAttributeString(Username_AttributeName, authorization.Username);
				writer.WriteAttributeString(Password_AttributeName, authorization.Password);

				writer.WriteEndElement();
			}

			writer.WriteEndElement();
			writer.WriteEndDocument();
		}

		File.WriteAllBytes(AuthorizationInfoFilePath, Encrypt(decryptedContent.ToString(), credentials));
	}

	#endregion Data Management

	#region Cryptography

	// see http://stackoverflow.com/questions/202011/

	private static string Salt {
		get {
			string salt = ConfigurationManager.AppSettings["salt"];
			if (string.IsNullOrEmpty(salt)) { throw new Exception("Configure the salt value in app.config"); }
			return salt;
		}
	}

	public static string Decrypt(byte[] encryptedAuthorizations, string credentials) {
		return Encoding.UTF8.GetString(CryptographyHelper(
			data: encryptedAuthorizations,
			credentials: credentials,
			direction: (aes) => aes.CreateDecryptor()));
	}
	public static byte[] Encrypt(string decryptedAuthorizations, string credentials) {
		return CryptographyHelper(
			data: Encoding.UTF8.GetBytes(decryptedAuthorizations),
			credentials: credentials,
			direction: (aes) => aes.CreateEncryptor());
	}
	private static byte[] CryptographyHelper(byte[] data, string credentials, Func<Aes, ICryptoTransform> direction) {
		PasswordDeriveBytes pdb = new PasswordDeriveBytes(credentials, Encoding.UTF8.GetBytes(Salt));

		using (Aes aes = AesManaged.Create()) {
			aes.Key = pdb.GetBytes(aes.KeySize / 8);
			aes.IV = pdb.GetBytes(aes.BlockSize / 8);

			using (MemoryStream outputStream = new MemoryStream()) {
				using (CryptoStream encryptionStream = new CryptoStream(outputStream, direction(aes), CryptoStreamMode.Write)) {
					encryptionStream.Write(data, 0, data.Length);
				}
				return outputStream.ToArray();
			}
		}
	}

	#endregion Cryptography
}
