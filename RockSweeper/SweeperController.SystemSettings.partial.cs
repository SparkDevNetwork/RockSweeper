namespace RockSweeper
{
    public partial class SweeperController
    {
        /// <summary>
        /// Sanitizes the application roots.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void SanitizeApplicationRoots()
        {
            SetGlobalAttributeValue( "InternalApplicationRoot", "http://rock.example.org" );
            SetGlobalAttributeValue( "PublicApplicationRoot", "http://www.example.org" );
        }

        /// <summary>
        /// Disables the communication transports.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableCommunicationTransports()
        {
            DisableComponentsOfType( "Rock.Communication.TransportComponent" );
        }

        /// <summary>
        /// Resets the existing communication transport configuration attribute values.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetCommunicationTransports()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Communication.TransportComponent" );
        }

        /// <summary>
        /// Configures Rock to use localhost SMTP email delivery.
        /// </summary>
        /// <param name="actionData">The action data.</param>
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
        public void DisableFinancialGateways()
        {
            SqlCommand( $@"UPDATE FG
SET FG.[IsActive] = 0
FROM[FinancialGateway] AS FG
INNER JOIN[EntityType] AS ET ON ET.[Id] = FG.[EntityTypeId]
WHERE ET.[Name] != 'Rock.Financial.TestGateway'" );
        }

        /// <summary>
        /// Resets the financial gateway configuration attributes.
        /// </summary>
        /// <param name="actionData">The action data.</param>
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
        public void DisableLocationServices()
        {
            DisableComponentsOfType( "Rock.Address.VerificationComponent" );
        }

        /// <summary>
        /// Resets the location services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetLocationServices()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Address.VerificationComponent" );
        }

        /// <summary>
        /// Disables the external storage providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
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
        public void DisableBackgroundCheckProviders()
        {
            DisableComponentsOfType( "Rock.Security.BackgroundCheckComponent" );
        }

        /// <summary>
        /// Resets the background check providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetBackgroundCheckProviders()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.BackgroundCheckComponent" );
        }

        /// <summary>
        /// Disables the signature document providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableSignatureDocumentProviders()
        {
            DisableComponentsOfType( "Rock.Security.DigitalSignatureComponent" );
        }

        /// <summary>
        /// Resets the signature document providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetSignatureDocumentProviders()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.DigitalSignatureComponent" );
            SetGlobalAttributeValue( "SignNowAccessToken", string.Empty );
        }

        /// <summary>
        /// Disables the phone systems.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisablePhoneSystems()
        {
            DisableComponentsOfType( "Rock.Pbx.PbxComponent" );
        }

        /// <summary>
        /// Resets the phone systems.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetPhoneSystems()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Pbx.PbxComponent" );
        }

        /// <summary>
        /// Resets the google API keys.
        /// </summary>
        public void ResetGoogleApiKeys()
        {
            SetGlobalAttributeValue( "GoogleAPIKey", string.Empty );
            SetGlobalAttributeValue( "core_GoogleReCaptchaSiteKey", string.Empty );
        }
    }
}
