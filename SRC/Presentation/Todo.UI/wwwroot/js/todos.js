requireAuth();

var deleteId    = null;
var allProjects = [];
var todoModal   = new bootstrap.Modal(document.getElementById('todoModal'));
var deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));

// ─── Init ─────────────────────────────────────────
async function init() {
    await loadProjectDropdowns();
    await loadTasks();
}

async function loadProjectDropdowns() {
    var res = await apiCall(API_BASE + '/project');
    if (!res || !res.success) return;
    allProjects = res.data;

    var opts = allProjects.map(function(p) {
        return '<option value="' + p.id + '">' + escHtml(p.name) + '</option>';
    }).join('');

    document.getElementById('filterProject').innerHTML =
        '<option value="">All Projects</option>' + opts;
    document.getElementById('tProject').innerHTML =
        '<option value="">Select project…</option>' + opts;
}

// ─── Load & render ────────────────────────────────
async function loadTasks() {
    var projectId = document.getElementById('filterProject').value;
    var status    = document.getElementById('filterStatus').value;
    var priority  = document.getElementById('filterPriority').value;
    var search    = document.getElementById('searchInput').value.trim();

    var url = API_BASE + '/todo?';
    if (projectId) url += 'projectId=' + encodeURIComponent(projectId) + '&';
    if (status)    url += 'status='    + encodeURIComponent(status)    + '&';
    if (priority)  url += 'priority='  + encodeURIComponent(priority)  + '&';
    if (search)    url += 'search='    + encodeURIComponent(search)    + '&';

    var res = await apiCall(url);
    if (!res) return;

    renderTasks(res.success ? res.data : []);
}

function renderTasks(tasks) {
    var grid  = document.getElementById('taskGrid');
    var empty = document.getElementById('emptyState');

    if (!tasks || tasks.length === 0) {
        grid.innerHTML = '';
        empty.classList.remove('d-none');
        document.getElementById('taskCount').textContent = '0 tasks';
        return;
    }

    empty.classList.add('d-none');
    document.getElementById('taskCount').textContent = tasks.length + ' task' + (tasks.length !== 1 ? 's' : '');

    var nextStatus = { Pending: 'InProgress', InProgress: 'Completed', Completed: 'Pending' };
    var nextLabel  = { Pending: 'Start', InProgress: 'Complete', Completed: 'Reopen' };
    var nextIcon   = { Pending: 'bi-play-fill', InProgress: 'bi-check-lg', Completed: 'bi-arrow-counterclockwise' };

    grid.innerHTML = tasks.map(function(t) {
        var done = t.isCompleted;
        var due  = formatDate(t.dueDate);
        var ns   = nextStatus[t.status]  || 'Pending';
        var nl   = nextLabel[t.status]   || 'Update';
        var ni   = nextIcon[t.status]    || 'bi-arrow-repeat';

        return '<div class="col-12 col-md-6 col-xl-4">'
            + '<div class="item-card' + (done ? ' opacity-75' : '') + '">'
            +   '<div class="card-row-top">'
            +     '<div style="overflow:hidden;flex:1">'
            +       '<div class="item-title' + (done ? ' text-decoration-line-through' : '') + '">' + escHtml(t.title) + '</div>'
            +       '<div style="font-size:.74rem;color:var(--txt-3);margin-top:2px"><i class="bi bi-folder2 me-1"></i>' + escHtml(t.projectName) + '</div>'
            +     '</div>'
            +     '<div class="action-btns">'
            +       '<button class="btn-act" onclick="openEditModal(' + t.id + ')" title="Edit"><i class="bi bi-pencil"></i></button>'
            +       '<button class="btn-act del" onclick="openDeleteModal(' + t.id + ',\'' + escHtml(t.title).replace(/'/g,"\\'") + '\')" title="Delete"><i class="bi bi-trash"></i></button>'
            +     '</div>'
            +   '</div>'
            +   (t.description ? '<div class="item-desc">' + escHtml(t.description) + '</div>' : '')
            +   '<div class="badge-row">'
            +     statusBadge(t.status)
            +     priorityBadge(t.priority)
            +   '</div>'
            +   '<div class="meta-row">'
            +     '<span><i class="bi bi-calendar3 me-1" style="font-size:.68rem"></i>' + due + '</span>'
            +     '<button class="btn-status" onclick="quickStatus(' + t.id + ',\'' + ns + '\')">'
            +       '<i class="bi ' + ni + '"></i> ' + nl
            +     '</button>'
            +   '</div>'
            + '</div></div>';
    }).join('');
}

// ─── Quick status toggle ──────────────────────────
async function quickStatus(id, newStatus) {
    var res = await apiCall(API_BASE + '/todo/' + id + '/status', 'PATCH', { status: newStatus });
    if (res && res.success) loadTasks();
}

// ─── Create / Edit modal ──────────────────────────
function openCreateModal() {
    document.getElementById('modalTitle').textContent = 'New Task';
    document.getElementById('saveTxt').textContent    = 'Save Task';
    document.getElementById('editId').value    = '';
    document.getElementById('tProject').value  = '';
    document.getElementById('tTitle').value    = '';
    document.getElementById('tDesc').value     = '';
    document.getElementById('tStatus').value   = 'Pending';
    document.getElementById('tPriority').value = 'Medium';
    document.getElementById('tDueDate').value  = '';
    document.getElementById('modalAlert').classList.add('d-none');
}

async function openEditModal(id) {
    var res = await apiCall(API_BASE + '/todo/' + id);
    if (!res || !res.success) return;
    var t = res.data;

    document.getElementById('modalTitle').textContent = 'Edit Task';
    document.getElementById('saveTxt').textContent    = 'Update';
    document.getElementById('editId').value    = t.id;
    document.getElementById('tProject').value  = t.projectId;
    document.getElementById('tTitle').value    = t.title;
    document.getElementById('tDesc').value     = t.description || '';
    document.getElementById('tStatus').value   = t.status;
    document.getElementById('tPriority').value = t.priority;
    document.getElementById('tDueDate').value  = t.dueDate ? t.dueDate.substring(0, 10) : '';
    document.getElementById('modalAlert').classList.add('d-none');

    todoModal.show();
}

async function saveTask() {
    var id        = document.getElementById('editId').value;
    var projectId = document.getElementById('tProject').value;
    var title     = document.getElementById('tTitle').value.trim();

    if (!projectId) { showModalAlert('Please select a project.', 'danger'); return; }
    if (!title)     { showModalAlert('Title is required.',        'danger'); return; }

    var payload = {
        projectId:   parseInt(projectId),
        title:       title,
        description: document.getElementById('tDesc').value.trim() || null,
        status:      document.getElementById('tStatus').value,
        priority:    document.getElementById('tPriority').value,
        dueDate:     document.getElementById('tDueDate').value || null
    };

    var url    = id ? API_BASE + '/todo/' + id : API_BASE + '/todo';
    var method = id ? 'PUT' : 'POST';
    var res    = await apiCall(url, method, payload);
    if (!res) return;

    if (res.success) { todoModal.hide(); loadTasks(); }
    else showModalAlert(res.message || 'Save failed.', 'danger');
}

// ─── Delete ───────────────────────────────────────
function openDeleteModal(id, name) {
    deleteId = id;
    document.getElementById('delName').textContent = name;
    deleteModal.show();
}

async function confirmDelete() {
    if (!deleteId) return;
    var res = await apiCall(API_BASE + '/todo/' + deleteId, 'DELETE');
    if (res && res.success) { deleteModal.hide(); loadTasks(); }
}

// ─── Filters ──────────────────────────────────────
function clearFilters() {
    document.getElementById('filterProject').value  = '';
    document.getElementById('filterStatus').value   = '';
    document.getElementById('filterPriority').value = '';
    document.getElementById('searchInput').value    = '';
    loadTasks();
}

function showModalAlert(msg, type) {
    var box = document.getElementById('modalAlert');
    box.className = 'alert alert-' + type + ' mb-3';
    box.textContent = msg;
}

// ─── Search debounce ──────────────────────────────
document.getElementById('searchInput').addEventListener('input', debounce(loadTasks, 300));

init();
