<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
    <#if section = "header">
        ${msg("loginAccountTitle")}
    <#elseif section = "form">
        <div id="kc-form">
            <div id="kc-form-wrapper">
                <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">

                    <#if !usernameHidden??>
                        <div class="tm-field">
                            <label for="username">
                                <#if !realm.loginWithEmailAllowed>${msg("username")}
                                <#elseif !realm.registrationEmailAsUsername>${msg("usernameOrEmail")}
                                <#else>${msg("email")}</#if>
                            </label>
                            <input
                                tabindex="1"
                                id="username"
                                name="username"
                                type="text"
                                autofocus
                                autocomplete="username"
                                value="${(login.username!'')}"
                                aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                            />
                            <#if messagesPerField.existsError('username','password')>
                                <div class="tm-error">${kcSanitize(messagesPerField.getFirstError('username','password'))?no_esc}</div>
                            </#if>
                        </div>
                    </#if>

                    <div class="tm-field">
                        <label for="password">${msg("password")}</label>
                        <div class="tm-password-wrapper">
                            <input
                                tabindex="2"
                                id="password"
                                name="password"
                                type="password"
                                autocomplete="current-password"
                                aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                            />
                        </div>
                    </div>

                    <div class="tm-form-options">
                        <#if realm.rememberMe && !usernameHidden??>
                            <label class="tm-remember">
                                <input tabindex="3" id="rememberMe" name="rememberMe" type="checkbox"
                                    <#if login.rememberMe??>checked</#if>
                                />
                                <span>${msg("rememberMe")}</span>
                            </label>
                        </#if>
                        <#if realm.resetPasswordAllowed>
                            <a tabindex="5" href="${url.loginResetCredentialsUrl}">${msg("doForgotPassword")}</a>
                        </#if>
                    </div>

                    <input type="hidden" id="id-hidden-input" name="credentialId"
                        <#if auth.selectedCredential?has_content>value="${auth.selectedCredential}"</#if>
                    />

                    <button tabindex="4" id="kc-login" name="login" type="submit">
                        ${msg("doLogIn")}
                    </button>
                </form>
            </div>
        </div>
    </#if>
</@layout.registrationLayout>
