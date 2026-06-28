// H3: API base sourced from window (set by _Layout.cshtml from server config)
const API_BASE = window.API_BASE || 'http://localhost:5001/api';

function showSpinner() { document.getElementById('loadingSpinner')?.classList.remove('d-none'); }
function hideSpinner() { document.getElementById('loadingSpinner')?.classList.add('d-none'); }

// H1: Logout calls the API to clear the HttpOnly cookie server-side
async function logout() {
    try {
        await fetch(API_BASE + '/auth/logout', { method: 'POST', credentials: 'include' });
    } catch (_) { }
    localStorage.removeItem('ica_username');
    window.location.href = '/Account/Login';
}

function toggleTheme() {
    var html  = document.documentElement;
    var cur   = html.getAttribute('data-bs-theme') || 'light';
    var next  = cur === 'light' ? 'dark' : 'light';
    html.setAttribute('data-bs-theme', next);
    localStorage.setItem('ica_theme', next);
    var btn = document.getElementById('themeBtn');
    if (btn) btn.innerHTML = next === 'dark'
        ? '<i class="bi bi-sun-fill"></i>'
        : '<i class="bi bi-moon-stars-fill"></i>';
}

// H1: No Authorization header — JWT is in HttpOnly cookie sent automatically
async function apiCall(url, method, body) {
    method = method || 'GET';
    showSpinner();
    try {
        var opts = {
            method: method,
            credentials: 'include', // sends the HttpOnly ica_auth cookie
            headers: { 'Content-Type': 'application/json' }
        };
        if (body) opts.body = JSON.stringify(body);
        var res = await fetch(url, opts);
        if (res.status === 401) { logout(); return null; }
        return await res.json();
    } catch (e) {
        console.error('API error:', e);
        return null;
    } finally {
        hideSpinner();
    }
}

// H1: Auth guard checks the non-sensitive username indicator in localStorage.
// Real enforcement is the API returning 401, which triggers logout() above.
function requireAuth() {
    if (!localStorage.getItem('ica_username')) {
        window.location.replace('/Account/Login');
    }
}

function debounce(fn, delay) {
    delay = delay || 300;
    var timer;
    return function() {
        var args = arguments;
        clearTimeout(timer);
        timer = setTimeout(function() { fn.apply(null, args); }, delay);
    };
}

function statusBadge(status) {
    var classMap = {
        Active:    'b-active',
        OnHold:    'b-onhold',
        Completed: 'b-completed',
        Pending:   'b-pending',
        InProgress:'b-inprogress'
    };
    var icons = {
        Active:    'bi-circle-fill',
        OnHold:    'bi-pause-circle-fill',
        Completed: 'bi-check-circle-fill',
        Pending:   'bi-clock-fill',
        InProgress:'bi-arrow-repeat'
    };
    var labels = { InProgress: 'In Progress', OnHold: 'On Hold' };
    var label = labels[status] || status;
    return '<span class="badge-pill ' + (classMap[status] || '') + '">'
         + '<i class="bi ' + (icons[status] || 'bi-circle') + '" style="font-size:.6rem"></i> '
         + escHtml(label) + '</span>';
}

function priorityBadge(priority) {
    var cls = { Low: 'b-low', Medium: 'b-medium', High: 'b-high' };
    var icons = { Low: 'bi-arrow-down', Medium: 'bi-dash', High: 'bi-arrow-up' };
    return '<span class="badge-pill ' + (cls[priority] || '') + '">'
         + '<i class="bi ' + (icons[priority] || '') + '" style="font-size:.6rem"></i> '
         + escHtml(priority) + '</span>';
}

function formatDate(d) {
    if (!d) return '<span style="color:var(--txt-3)">—</span>';
    var dt = new Date(d);
    var now = new Date();
    var diff = Math.ceil((dt - now) / 86400000);
    var str = dt.toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' });
    if (diff < 0)  return '<span style="color:#ef4444">' + str + '</span>';
    if (diff <= 3) return '<span style="color:#f59e0b">' + str + '</span>';
    return '<span>' + str + '</span>';
}

function escHtml(str) {
    return String(str || '')
        .replace(/&/g,'&amp;')
        .replace(/</g,'&lt;')
        .replace(/>/g,'&gt;')
        .replace(/"/g,'&quot;')
        .replace(/'/g,'&#39;');
}
