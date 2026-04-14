<#macro registrationLayout bodyClass="" displayInfo=false displayMessage=true displayRequiredFields=false>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="robots" content="noindex, nofollow">
    <title>${msg("loginTitle", realm.displayName!'LATAM Platform')}</title>
    <link rel="stylesheet" href="${url.resourcesPath}/css/login.css"/>
</head>
<body>

<div class="tm-page">
    <div class="tm-card">

        <div class="tm-logo">
            <img src="${url.resourcesPath}/img/logo.png" alt="Ticketmaster"/>
        </div>

        <#if displayMessage && message?has_content>
            <#if message.type != 'warning' || !isAppInitiatedAction??>
                <div class="tm-alert tm-alert-${message.type!'info'}">
                    ${kcSanitize(message.summary)?no_esc}
                </div>
            </#if>
        </#if>

        <#nested "form">

        <#if displayInfo>
            <div class="tm-info">
                <#nested "info">
            </div>
        </#if>

    </div>
</div>

</body>
</html>
</#macro>
