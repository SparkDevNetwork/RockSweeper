using System.ComponentModel;

using RockSweeper.Attributes;

namespace RockSweeper
{
    public partial class SweeperController
    {
        /// <summary>
        /// Sanitizes the application roots.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "dcb7cde1-7764-4ce4-bbb8-13001c4cc9dd" )]
        [Title( "Sanitize Application Roots" )]
        [Description( "Modifies the PublicApplicationRoot and InternalApplicationRoot to safe values." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        public void SanitizeApplicationRoots()
        {
            SetGlobalAttributeValue( "InternalApplicationRoot", "http://rock.example.org" );
            SetGlobalAttributeValue( "PublicApplicationRoot", "http://www.example.org" );
        }

        /// <summary>
        /// Disables the communication transports.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "b55c9a45-763d-45d7-8a77-dc0a93fc542b" )]
        [Title( "Disable Communication Transports" )]
        [Description( "Updates the Rock configuration to ensure that all communication transports are disabled." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        [RequiresRockWeb]
        public void DisableCommunicationTransports()
        {
            DisableComponentsOfType( "Rock.Communication.TransportComponent" );
        }

        /// <summary>
        /// Resets the existing communication transport configuration attribute values.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "c1770744-9498-4b35-a172-f64ad00f74c0" )]
        [Title( "Reset Communication Transports" )]
        [Description( "Resets all transport configuration to system default values." )]
        [Category( "System Settings" )]
        [RequiresRockWeb]
        public void ResetCommunicationTransports()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Communication.TransportComponent" );
        }

        /// <summary>
        /// Configures Rock to use localhost SMTP email delivery.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "24ea3e6e-04ab-4896-9174-bc275f67f766" )]
        [Title( "Configure For Localhost SMTP" )]
        [Description( "Updates the communication settings to use a localhost SMTP server." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        [AfterAction( nameof( DisableCommunicationTransports ) )]
        [AfterAction( nameof( ResetCommunicationTransports ) )]
        public void ConfigureForLocalhostSmtp()
        {
            //
            // Setup the Email medium.
            //
            SetComponentAttributeValue( "Rock.Communication.Medium.Email", "Active", "True" );
            SetComponentAttributeValue( "Rock.Communication.Medium.Email", "TransportContainer", "1fef44b2-8685-4001-be5b-8a059bc65430" );

            //
            // Set SMTP Transport to Active.
            //
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Active", "True" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Server", "localhost" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Port", "25" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "UserName", "" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Password", "" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "UseSSL", "False" );
        }

        /// <summary>
        /// Disables the financial gateways.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "47e49503-5a38-4781-8695-4f69d3296e7e" )]
        [Title( "Disable Financial Gateways" )]
        [Description( "Updates the Rock configuration to ensure that all financial gateways except the test gateway are disabled." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        public void DisableFinancialGateways()
        {
            SqlCommand( $@"UPDATE FG
SET FG.[IsActive] = 0
FROM [FinancialGateway] AS FG
INNER JOIN[EntityType] AS ET ON ET.[Id] = FG.[EntityTypeId]
WHERE ET.[Name] != 'Rock.Financial.TestGateway'" );
        }

        /// <summary>
        /// Resets the financial gateway configuration attributes.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "57da3ba1-4166-446a-a998-be7229a32b52" )]
        [Title( "Reset Financial Gateways" )]
        [Description( "Resets all financial gateways except the test gateway to system default values." )]
        [Category( "System Settings" )]
        public void ResetFinancialGateways()
        {
            int? entityTypeId = GetEntityTypeId( "Rock.Model.FinancialGateway" );

            SqlCommand( $@"DELETE AV
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
INNER JOIN [FinancialGateway] AS FG ON FG.[Id] = AV.[EntityId]
INNER JOIN [EntityType] AS ET ON ET.[Id] = FG.[EntityTypeId]
WHERE A.[EntityTypeId] = { entityTypeId.Value } AND ET.[Name] != 'Rock.Financial.TestGateway'" );
        }

        /// <summary>
        /// Disables the external authentication services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "47f1e5dd-01cb-46bd-8f2a-a097b171c070" )]
        [Title( "Disable External Authentication Services" )]
        [Description( "Updates the Rock configuration to ensure that authentication services other than database, AD and PIN are disabled." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        [RequiresRockWeb]
        public void DisableExternalAuthenticationServices()
        {
            DisableComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.ActiveDirectory",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Resets the external authentication services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "98e65e7a-32cb-4783-af07-d0605c6ca9a4" )]
        [Title( "Reset External Authentication Services" )]
        [Description( "Resets authentication services other than database, AD and PIN to system default values." )]
        [Category( "System Settings" )]
        [RequiresRockWeb]
        public void ResetExternalAuthenticationServices()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.ActiveDirectory",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Disables the authentication services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "a68b25fb-6ec2-4d3d-b1de-16aae76c47d6" )]
        [Title( "Disable Authentication Services" )]
        [Description( "Updates the Rock configuration to ensure that authentication services other than database and PIN are disabled." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        [RequiresRockWeb]
        public void DisableAuthenticationServices()
        {
            DisableComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Resets the authentication services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "29386c4e-38da-4e83-8d8d-058f735a087c" )]
        [Title( "Reset Authentication Services" )]
        [Description( "Resets authentication services other than database and PIN to system default values." )]
        [Category( "System Settings" )]
        [RequiresRockWeb]
        public void ResetAuthenticationServices()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Disables the location services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "8a6cb4e7-cc47-4d0d-8358-2373424bace1" )]
        [Title( "Disable Location Services" )]
        [Description( "Updates the Rock configuration to ensure that all location services are disabled." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        [RequiresRockWeb]
        public void DisableLocationServices()
        {
            DisableComponentsOfType( "Rock.Address.VerificationComponent" );
        }

        /// <summary>
        /// Resets the location services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "ff2e4638-87ba-40eb-bda9-039d8277be8b" )]
        [Title( "Reset Location Services" )]
        [Description( "Resets all location services to system default values." )]
        [Category( "System Settings" )]
        [RequiresRockWeb]
        public void ResetLocationServices()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Address.VerificationComponent" );
        }

        /// <summary>
        /// Disables the external storage providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "604d8e13-9f32-4542-8916-13e204695838" )]
        [Title( "Disable External Storage Providers" )]
        [Description( "Updates the Rock configuration to ensure that storage providers other than database and filesystem are disabled." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        [RequiresRockWeb]
        public void DisableExternalStorageProviders()
        {
            DisableComponentsOfType( "Rock.Storage.ProviderComponent", new[]
            {
                "Rock.Storage.Provider.Database",
                "Rock.Storage.Provider.FileSystem"
            } );
        }

        /// <summary>
        /// Resets the external storage providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "405bb345-d1a4-4bc3-861c-9fab90c0c2da" )]
        [Title( "Reset External Storage Providers" )]
        [Description( "Resets storage providers other than database and filesystem to system default values." )]
        [Category( "System Settings" )]
        [RequiresRockWeb]
        public void ResetExternalStorageProviders()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Storage.ProviderComponent", new[]
            {
                "Rock.Storage.Provider.Database",
                "Rock.Storage.Provider.FileSystem"
            } );
        }

        /// <summary>
        /// Disables the background check providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "903e3037-e614-41ad-a663-babd069d7927" )]
        [Title( "Disable Background Check Providers" )]
        [Description( "Updates the Rock configuration to ensure that all background check providers are disabled." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        [RequiresRockWeb]
        public void DisableBackgroundCheckProviders()
        {
            DisableComponentsOfType( "Rock.Security.BackgroundCheckComponent" );
        }

        /// <summary>
        /// Resets the background check providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "4d9f9857-429b-4e1b-8833-8e702bbc7952" )]
        [Title( "Reset Background Check Providers" )]
        [Description( "Resets all background check providers to system default values." )]
        [Category( "System Settings" )]
        [RequiresRockWeb]
        public void ResetBackgroundCheckProviders()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.BackgroundCheckComponent" );
        }

        /// <summary>
        /// Disables the signature document providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "0d1246a9-8a19-4658-a1c5-c8804e8bfeab" )]
        [Title( "Disable Signature Document Providers" )]
        [Description( "Updates the Rock configuration to ensure that all signed document providers are disabled." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        [RequiresRockWeb]
        public void DisableSignatureDocumentProviders()
        {
            DisableComponentsOfType( "Rock.Security.DigitalSignatureComponent" );
        }

        /// <summary>
        /// Resets the signature document providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "59fd05b1-0263-4442-9d63-7491e254bcd1" )]
        [Title( "Reset Signature Document Providers" )]
        [Description( "Resets all signed document providers to system default values." )]
        [Category( "System Settings" )]
        [RequiresRockWeb]
        public void ResetSignatureDocumentProviders()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.DigitalSignatureComponent" );
            SetGlobalAttributeValue( "SignNowAccessToken", string.Empty );
        }

        /// <summary>
        /// Disables the phone systems.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "9c4bf948-5dd7-4bc0-ac2e-0cfb2493d02f" )]
        [Title( "Disable Phone Systems" )]
        [Description( "Updates the Rock configuration to ensure that all phone systems are disabled." )]
        [Category( "System Settings" )]
        [DefaultValue( true )]
        [RequiresRockWeb]
        public void DisablePhoneSystems()
        {
            DisableComponentsOfType( "Rock.Pbx.PbxComponent" );
        }

        /// <summary>
        /// Resets the phone systems.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "afee211e-2a2d-4d85-a428-81ac95637ed6" )]
        [Title( "Reset Phone Systems" )]
        [Description( "Resets all phone system settings to system default values." )]
        [Category( "System Settings" )]
        [RequiresRockWeb]
        public void ResetPhoneSystems()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Pbx.PbxComponent" );
        }

        /// <summary>
        /// Resets the google API keys.
        /// </summary>
        [ActionId( "d5e352c6-b1bf-405a-9934-6f875725a5c1" )]
        [Title( "Reset Google API Keys" )]
        [Description( "Clears the Google API keys stored in global attributes." )]
        [Category( "System Settings" )]
        public void ResetGoogleApiKeys()
        {
            SetGlobalAttributeValue( "GoogleAPIKey", string.Empty );
            SetGlobalAttributeValue( "core_GoogleReCaptchaSiteKey", string.Empty );
        }
    }
}
