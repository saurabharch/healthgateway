resource "keycloak_oidc_identity_provider" "bcsc" {
  realm                         = var.keycloak_realm
  alias                         = "bcsc"
  display_name                  = "BCSC"
  enabled                       = true
  store_token                   = false
  trust_email                   = true
  hide_on_login_page            = true
  first_broker_login_flow_alias = keycloak_authentication_flow.first_login.alias
  sync_mode                     = "FORCE"
  authorization_url             = "${var.keycloak_idp_bcsc_base_url}${var.keycloak_idp_bcsc_auth_path}"
  token_url                     = "${var.keycloak_idp_bcsc_base_url}${var.keycloak_idp_bcsc_token_path}"
  backchannel_supported         = false
  user_info_url                 = "${var.keycloak_idp_bcsc_base_url}${var.keycloak_idp_bcsc_userinfo_path}"
  client_id                     = var.keycloak_idp_bcsc_client_id
  client_secret                 = var.keycloak_idp_bcsc_client_secret
  issuer                        = "${var.keycloak_idp_bcsc_base_url}${var.keycloak_idp_bcsc_issuer_path}"
  default_scopes                = "openid profile email"
  validate_signature            = true
  jwks_url                      = "${var.keycloak_idp_bcsc_base_url}${var.keycloak_idp_bcsc_jwks_path}"
  extra_config = {
    "clientAuthMethod" = "client_secret_post"
    "prompt"           = "login"
  }
}

resource "keycloak_attribute_importer_identity_provider_mapper" "bcsc_given_name" {
  realm                   = var.keycloak_realm
  name                    = "given_name"
  claim_name              = "given_names"
  identity_provider_alias = keycloak_oidc_identity_provider.bcsc.alias
  user_attribute          = "firstName"
  extra_config = {
    syncMode = "INHERIT"
  }
}

resource "keycloak_hardcoded_attribute_identity_provider_mapper" "bcsc_idp" {
  realm                   = var.keycloak_realm
  name                    = "idp"
  identity_provider_alias = keycloak_oidc_identity_provider.bcsc.alias
  attribute_name          = "idp"
  attribute_value         = "BCSC"
  user_session            = false
  extra_config = {
    syncMode = "INHERIT"
  }
}

resource "keycloak_attribute_importer_identity_provider_mapper" "bcsc_email" {
  realm                   = var.keycloak_realm
  name                    = "email"
  claim_name              = "email"
  identity_provider_alias = keycloak_oidc_identity_provider.bcsc.alias
  user_attribute          = "email"
  extra_config = {
    syncMode = "INHERIT"
  }
}

resource "keycloak_attribute_importer_identity_provider_mapper" "bcsc_display_name" {
  realm                   = var.keycloak_realm
  name                    = "display_name"
  claim_name              = "display_name"
  identity_provider_alias = keycloak_oidc_identity_provider.bcsc.alias
  user_attribute          = "fullName"
  extra_config = {
    syncMode = "INHERIT"
  }
}

resource "keycloak_attribute_importer_identity_provider_mapper" "bcsc_family_name" {
  realm                   = var.keycloak_realm
  name                    = "family_name"
  claim_name              = "family_name"
  identity_provider_alias = keycloak_oidc_identity_provider.bcsc.alias
  user_attribute          = "lastName"
  extra_config = {
    syncMode = "INHERIT"
  }
}

resource "keycloak_attribute_importer_identity_provider_mapper" "bcsc_username" {
  realm                   = var.keycloak_realm
  name                    = "username"
  claim_name              = "sub"
  identity_provider_alias = keycloak_oidc_identity_provider.bcsc.alias
  user_attribute          = "username"
  extra_config = {
    syncMode = "INHERIT"
  }
}

resource "keycloak_attribute_importer_identity_provider_mapper" "bcsc_hdid" {
  realm                   = var.keycloak_realm
  name                    = "hdid"
  claim_name              = "sub"
  identity_provider_alias = keycloak_oidc_identity_provider.bcsc.alias
  user_attribute          = "hdid"
  extra_config = {
    syncMode = "INHERIT"
  }
}

resource "keycloak_attribute_importer_identity_provider_mapper" "bcsc_iasenv" {
  realm                   = var.keycloak_realm
  name                    = "iasenv"
  claim_name              = "authoritative_party_identifier"
  identity_provider_alias = keycloak_oidc_identity_provider.bcsc.alias
  user_attribute          = "iasenv"
  extra_config = {
    syncMode = "INHERIT"
  }
}