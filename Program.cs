// License: http://opensource.org/licenses/GPL-3.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

public static class Program {
	public static int Main(string[] args) {
		try {
			Operation operation = GetOperation(args);
			string service = GetService(args);
			object credentials = GetCredentials();

			List<Authorization> authorizations = LoadAuthorizations(credentials);
			switch (operation) {
				case Operation.Get: {
						Authorization info = authorizations.Find(new Predicate<Authorization>(delegate(Authorization auth) { return auth.Service == service; }));
						CheckApplications();
						ExposeAuthorizationInfo(info);
						break;
					}
				case Operation.Delete: {
						Authorization info = authorizations.Find(new Predicate<Authorization>(delegate(Authorization auth) { return auth.Service == service; }));
						authorizations.Remove(info);
						SaveAuthorizations(authorizations, credentials);
						break;
					}
				case Operation.Add: {
						authorizations.Add(GetAuthorizationInfo(service));
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

	public static object GetCredentials() {
		// TODO: implement asking for password
		return new object();
	}

	public static Authorization GetAuthorizationInfo(string service) {
		Console.WriteLine("Please enter authorization information for {0} service", service);
		Console.Write("User name : ");
		string username = Console.ReadLine();
		Console.WriteLine("Password : ");
		// TODO: do not show password
		string password = Console.ReadLine();
		return new Authorization(service, username, password);
	}

	public static void ExposeAuthorizationInfo(Authorization authorizationInfo) {
		Console.WriteLine("Exposing authorization information for {0} service", authorizationInfo.Service);
		if (!string.IsNullOrEmpty(authorizationInfo.Username)) {
			Console.WriteLine("User name is on clipboard");
			SetClipboardContent(authorizationInfo.Username);
		}
		Console.WriteLine("Password is on clipboard");
		SetClipboardContent(authorizationInfo.Password);
	}

	#endregion User Interactions

	#region Operating System Interactions

	public static void SetClipboardContent(string content) {
		// TODO: interact with actual clipboard
		Console.Error.WriteLine("Clipboard content set to : {0}", content);
	}

	public static void CheckApplications() {
		// TODO: check for some applications here (clip board managers)
		// if found, show a warning that they should be turned off, and request an enter to proceed
	}

	#endregion Operating System Interactions

	#region Cryptography

	public static string Decrypt(byte[] encryptedAuthorizationInfo, object credentials) {
		// TODO: implement actual decryption
		return Encoding.UTF8.GetString(encryptedAuthorizationInfo);
	}
	public static byte[] Encrypt(string decryptedAuthorizationInfo, object credentials) {
		// TODO: implement actual encryption
		return Encoding.UTF8.GetBytes(decryptedAuthorizationInfo);
	}

	#endregion Cryptography

	#region Data Management

	/*
	 * Authorization file info structure (Xml):
	 * <root>
	 *     <info service="string" username="username" password="password"/>
	 *     <info ...
	 *     ...
	 * </root>
	 * 
	 * */

	public const string AuthorizationInfoFilePath = "passman.data";

	public static List<Authorization> LoadAuthorizations(object credentials) {
		List<Authorization> result = new List<Authorization>();
		if (!File.Exists(AuthorizationInfoFilePath)) { return result; }

		string decryptedContent = Decrypt(File.ReadAllBytes(AuthorizationInfoFilePath), credentials);
		XmlDocument structuredContent = new XmlDocument();
		structuredContent.LoadXml(decryptedContent);

		foreach (XmlNode authorizationEntry in structuredContent.SelectNodes("/*/info")) {
			result.Add(new Authorization(
				authorizationEntry.Attributes["service"].Value,
				authorizationEntry.Attributes["username"].Value,
				authorizationEntry.Attributes["password"].Value));
		}

		return result;
	}

	public static void SaveAuthorizations(List<Authorization> authorizationInfo, object credentials) {
		throw new NotImplementedException();
	}

	#endregion Data Management
}

public enum Operation {
	Get = 0,
	Delete,
	Add,
}

// TODO: come up with better name
public class Authorization {
	public Authorization(string service, string username, string password) {
		this.Service = service;
		this.Username = username;
		this.Password = password;
	}

	public readonly string Service;
	public readonly string Username;
	public readonly string Password;
}
