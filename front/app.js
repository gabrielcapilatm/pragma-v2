const CONFIG = {
  keycloakUrl: 'http://localhost:8080',
  realm: 'latam-platform',
  clientId: 'latam-api',
  redirectUri: 'http://localhost:3000',
  apiUrl: 'http://localhost:5288'
};

const COUNTRY_NAMES = { BR: 'Brasil 🇧🇷', AR: 'Argentina 🇦🇷', CL: 'Chile 🇨🇱' };
const CURRENCIES = { BR: 'BRL', AR: 'ARS', CL: 'CLP' };

// --- DOM ---
const loginScreen  = document.getElementById('login-screen');
const appScreen    = document.getElementById('app-screen');
const loginBtn     = document.getElementById('login-btn');
const loginError   = document.getElementById('login-error');
const logoutBtn    = document.getElementById('logout-btn');
const userNameEl   = document.getElementById('user-name');
const userTenantEl = document.getElementById('user-tenant');
const countryLabel = document.getElementById('country-label');
const productsBody = document.getElementById('products-body');

// --- PKCE helpers ---
function base64urlEncode(buffer) {
  return btoa(String.fromCharCode(...new Uint8Array(buffer)))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
}

function generateVerifier() {
  const buf = new Uint8Array(32);
  crypto.getRandomValues(buf);
  return base64urlEncode(buf);
}

async function generateChallenge(verifier) {
  const encoded = new TextEncoder().encode(verifier);
  const digest = await crypto.subtle.digest('SHA-256', encoded);
  return base64urlEncode(digest);
}

// --- Login: redireciona para o Keycloak ---
loginBtn.addEventListener('click', async () => {
  const verifier = generateVerifier();
  const challenge = await generateChallenge(verifier);
  const state = generateVerifier(); // valor aleatório para proteção CSRF

  sessionStorage.setItem('pkce_verifier', verifier);
  sessionStorage.setItem('pkce_state', state);

  const params = new URLSearchParams({
    response_type: 'code',
    client_id: CONFIG.clientId,
    redirect_uri: CONFIG.redirectUri,
    scope: 'openid profile',
    code_challenge: challenge,
    code_challenge_method: 'S256',
    state
  });

  window.location.href =
    `${CONFIG.keycloakUrl}/realms/${CONFIG.realm}/protocol/openid-connect/auth?${params}`;
});

// --- Callback: Keycloak redirecionou de volta com o code ---
async function handleCallback() {
  const params = new URLSearchParams(window.location.search);
  const code = params.get('code');
  const state = params.get('state');

  if (!code) return; // não é um callback, página normal

  // Limpa os parâmetros da URL sem recarregar
  window.history.replaceState({}, '', CONFIG.redirectUri);

  const savedState = sessionStorage.getItem('pkce_state');
  if (state !== savedState) {
    showError('Falha de segurança: state inválido.');
    return;
  }

  const verifier = sessionStorage.getItem('pkce_verifier');
  sessionStorage.removeItem('pkce_verifier');
  sessionStorage.removeItem('pkce_state');

  try {
    loginBtn.disabled = true;
    loginBtn.textContent = 'Autenticando...';

    const token = await exchangeCodeForToken(code, verifier);
    const payload = parseJwt(token);

    sessionStorage.setItem('access_token', token);
    showApp(payload);
    loadProducts(token, payload.tenant);
  } catch (err) {
    showError(err.message);
    loginBtn.disabled = false;
    loginBtn.textContent = 'Entrar com Keycloak';
  }
}

// --- Troca o code pelo token ---
async function exchangeCodeForToken(code, verifier) {
  const body = new URLSearchParams({
    grant_type: 'authorization_code',
    client_id: CONFIG.clientId,
    redirect_uri: CONFIG.redirectUri,
    code,
    code_verifier: verifier
  });

  const res = await fetch(
    `${CONFIG.keycloakUrl}/realms/${CONFIG.realm}/protocol/openid-connect/token`,
    { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body }
  );

  const data = await res.json();
  if (!res.ok) throw new Error(data.error_description || 'Erro ao obter token.');
  return data.access_token;
}

// --- Logout ---
logoutBtn.addEventListener('click', () => {
  const token = sessionStorage.getItem('access_token');
  sessionStorage.removeItem('access_token');

  appScreen.classList.add('hidden');
  loginScreen.classList.remove('hidden');
  loginBtn.disabled = false;
  loginBtn.textContent = 'Entrar com Keycloak';

  // Encerra a sessão no Keycloak também
  const params = new URLSearchParams({ post_logout_redirect_uri: CONFIG.redirectUri, client_id: CONFIG.clientId });
  window.location.href =
    `${CONFIG.keycloakUrl}/realms/${CONFIG.realm}/protocol/openid-connect/logout?${params}`;
});

// --- UI ---
function showApp(payload) {
  const tenant = payload.tenant || '??';
  userNameEl.textContent = payload.name || payload.preferred_username;
  userTenantEl.textContent = tenant;
  userTenantEl.className = `tenant-badge tenant-${tenant.toLowerCase()}`;
  countryLabel.textContent = COUNTRY_NAMES[tenant] || tenant;
  loginScreen.classList.add('hidden');
  appScreen.classList.remove('hidden');
}

function showError(msg) {
  loginError.textContent = msg;
  loginError.classList.remove('hidden');
}

function parseJwt(token) {
  const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
  return JSON.parse(atob(base64));
}

// --- API ---
async function loadProducts(token, tenant) {
  try {
    const res = await fetch(`${CONFIG.apiUrl}/api/products`, {
      headers: { Authorization: `Bearer ${token}` }
    });

    if (!res.ok) throw new Error(`HTTP ${res.status}`);

    const products = await res.json();
    renderProducts(products, tenant);
  } catch (err) {
    productsBody.innerHTML =
      `<tr><td colspan="3" class="error">Erro ao carregar produtos: ${err.message}</td></tr>`;
  }
}

function renderProducts(products, tenant) {
  if (products.length === 0) {
    productsBody.innerHTML = '<tr><td colspan="3" class="loading">Nenhum produto encontrado.</td></tr>';
    return;
  }

  const currency = CURRENCIES[tenant] || 'USD';
  const fmt = new Intl.NumberFormat('pt-BR', { style: 'currency', currency });

  productsBody.innerHTML = products.map(p => `
    <tr>
      <td>${p.name}</td>
      <td><span class="category-badge">${p.category}</span></td>
      <td class="price">${fmt.format(p.price)}</td>
    </tr>
  `).join('');
}

// --- Ponto de entrada ---
// Verifica se a sessão já existe (reload da página)
const savedToken = sessionStorage.getItem('access_token');
if (savedToken) {
  try {
    const payload = parseJwt(savedToken);
    // Verifica se o token ainda não expirou
    if (payload.exp * 1000 > Date.now()) {
      showApp(payload);
      loadProducts(savedToken, payload.tenant);
    } else {
      sessionStorage.removeItem('access_token');
    }
  } catch {
    sessionStorage.removeItem('access_token');
  }
}

// Processa callback do Keycloak se houver ?code= na URL
handleCallback();
