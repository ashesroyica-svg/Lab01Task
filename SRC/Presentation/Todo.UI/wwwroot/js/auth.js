const API_BASE = 'http://localhost:5001/api';

function showSpinner() { document.getElementById('loadingSpinner')?.classList.remove('d-none'); }
function hideSpinner() { document.getElementById('loadingSpinner')?.classList.add('d-none'); }

function logout() {
    localStorage.removeItem('ica_token');
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

async function apiCall(url, method, body) {
    method = method || 'GET';
    var token = localStorage.getItem('ica_token');
    showSpinner();
    try {
        var opts = {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': token ? ('Bearer ' + token) : ''
            }
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

function requireAuth() {
    if (!localStorage.getItem('ica_token')) {
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
         + label + '</span>';
}

function priorityBadge(priority) {
    var cls = { Low: 'b-low', Medium: 'b-medium', High: 'b-high' };
    var icons = { Low: 'bi-arrow-down', Medium: 'bi-dash', High: 'bi-arrow-up' };
    return '<span class="badge-pill ' + (cls[priority] || '') + '">'
         + '<i class="bi ' + (icons[priority] || '') + '" style="font-size:.6rem"></i> '
         + priority + '</span>';
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
        .replace(/"/g,'&quot;');
}
