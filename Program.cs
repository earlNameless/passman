// License: http://opensource.org/licenses/GPL-3.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

// TODO: target .NET 4.0? it will make some argument passing clearer

public static class Program {
	public static int Main(string[] args) {
		try {
			Operation operation = GetOperation(args);
			string service = GetService(args);
			string credentials = GetCredentials();

			List<Authorization> authorizations = LoadAuthorizations(credentials);
			switch (operation) {
				case Operation.Get: {
						Authorization auth = Authorization.FindByService(authorizations, service);
						CheckApplications();
						ExposeAuthorizationInfo(auth);
						break;
					}
				case Operation.Add: {
						authorizations.Add(GetAuthorizationInfo(service));
						SaveAuthorizations(authorizations, credentials);
						break;
					}
				case Operation.Remove: {
						Authorization auth = Authorization.FindByService(authorizations, service);
						authorizations.Remove(auth);
						SaveAuthorizations(authorizations, credentials);
						break;
					}
				default: {
						throw new InvalidOperationException(string.Format("Unknown operation given : {0}", operation.ToString()));
					}
			}

			return 0;
		} catch (Exception ex) {
			Console.Error.WriteLine(ex.Message);
			return -1;
		}
	}

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

	public static string GetInputWithoutEcho() {
		// TODO: implement getting input without echo
		return Console.ReadLine();
	}

	public static string GetCredentials() {
		Console.Write("Enter main password : ");
		return GetInputWithoutEcho();
	}

	public static Authorization GetAuthorizationInfo(string service) {
		Console.WriteLine("Please enter authorization information for {0} service.", service);
		Console.Write("User name : ");
		string username = Console.ReadLine();
		Console.WriteLine("Password : ");
		string password = GetInputWithoutEcho();
		return new Authorization(service, username, password);
	}

	public static void ExposeAuthorizationInfo(Authorization authorizationInfo) {
		Console.WriteLine("Exposing authorization information for {0} service.", authorizationInfo.Service);
		if (authorizationInfo.Username == string.Empty) {
			Console.WriteLine("User name is on clipboard.");
			SetClipboardContent(authorizationInfo.Username);
		}
		Console.WriteLine("Password is on clipboard.");
		SetClipboardContent(authorizationInfo.Password);
	}

	#endregion User Interactions

	#region Operating System Interactions

	public static void SetClipboardContent(string content) {
		// TODO: interact with actual clipboard
		Console.Error.WriteLine("Clipboard content set to : {0}.", content);
	}

	public static void CheckApplications() {
		// TODO: check for some applications here (clip board managers)
		// if found, show a warning that they should be turned off, and request an enter to proceed
	}

	#endregion Operating System Interactions

	#region Cryptography

	public static string Decrypt(byte[] encryptedAuthorizationInfo, string credentials) {
		// TODO: implement actual decryption
		return Encoding.UTF8.GetString(encryptedAuthorizationInfo);
	}
	public static byte[] Encrypt(string decryptedAuthorizationInfo, string credentials) {
		// TODO: implement actual encryption
		return Encoding.UTF8.GetBytes(decryptedAuthorizationInfo);
	}

	#endregion Cryptography

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
				authorizationEntry.Attributes[Service_AttributeName].Value,
				authorizationEntry.Attributes[Username_AttributeName].Value,
				authorizationEntry.Attributes[Password_AttributeName].Value));
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
}

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
