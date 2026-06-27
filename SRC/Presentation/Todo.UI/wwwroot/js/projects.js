requireAuth();

var deleteId = null;
var projectModal = new bootstrap.Modal(document.getElementById('projectModal'));
var deleteModal  = new bootstrap.Modal(document.getElementById('deleteModal'));

// ─── Load & render ───────────────────────────────
async function loadProjects() {
    var status   = document.getElementById('filterStatus').value;
    var priority = document.getElementById('filterPriority').value;
    var search   = document.getElementById('searchInput').value.trim();

    var url = API_BASE + '/project?';
    if (status)   url += 'status='   + encodeURIComponent(status)   + '&';
    if (priority) url += 'priority=' + encodeURIComponent(priority) + '&';
    if (search)   url += 'search='   + encodeURIComponent(search)   + '&';

    var res = await apiCall(url);
    if (!res) return;

    renderProjects(res.success ? res.data : []);
}

function renderProjects(projects) {
    var grid  = document.getElementById('projectGrid');
    var empty = document.getElementById('emptyState');

    if (!projects || projects.length === 0) {
        grid.innerHTML = '';
        empty.classList.remove('d-none');
        document.getElementById('projectCount').textContent = '0 projects';
        return;
    }

    empty.classList.add('d-none');
    document.getElementById('projectCount').textContent = projects.length + ' project' + (projects.length !== 1 ? 's' : '');

    grid.innerHTML = projects.map(function(p) {
        var due = p.dueDate ? formatDate(p.dueDate) : '<span style="color:var(--txt-3)">No due date</span>';
        return '<div class="col-12 col-md-6 col-xl-4">'
            + '<div class="item-card">'
            +   '<div class="card-row-top">'
            +     '<div style="overflow:hidden;flex:1">'
            +       '<div class="item-title">' + escHtml(p.name) + '</div>'
            +     '</div>'
            +     '<div class="action-btns">'
            +       '<button class="btn-act" onclick="openEditModal(' + p.id + ')" title="Edit"><i class="bi bi-pencil"></i></button>'
            +       '<button class="btn-act del" onclick="openDeleteModal(' + p.id + ',\'' + escHtml(p.name).replace(/'/g,"\\'") + '\')" title="Delete"><i class="bi bi-trash"></i></button>'
            +     '</div>'
            +   '</div>'
            +   (p.description ? '<div class="item-desc">' + escHtml(p.description) + '</div>' : '<div class="item-desc" style="color:var(--txt-3);font-style:italic">No description</div>')
            +   '<div class="badge-row">'
            +     statusBadge(p.status)
            +     priorityBadge(p.priority)
            +     '<span class="project-task-count"><i class="bi bi-list-task"></i> ' + (p.taskCount || 0) + '</span>'
            +   '</div>'
            +   '<div class="meta-row">'
            +     '<span><i class="bi bi-calendar3 me-1" style="font-size:.7rem"></i>' + due + '</span>'
            +     '<span style="font-size:.7rem;color:var(--txt-3)">' + new Date(p.createdDate).toLocaleDateString('en-GB',{day:'2-digit',month:'short'}) + '</span>'
            +   '</div>'
            + '</div></div>';
    }).join('');
}

// ─── Create / Edit modal ─────────────────────────
function openCreateModal() {
    document.getElementById('modalTitle').textContent = 'New Project';
    document.getElementById('saveTxt').textContent    = 'Save Project';
    document.getElementById('editId').value    = '';
    document.getElementById('pName').value     = '';
    document.getElementById('pDesc').value     = '';
    document.getElementById('pStatus').value   = 'Active';
    document.getElementById('pPriority').value = 'Medium';
    document.getElementById('pDueDate').value  = '';
    document.getElementById('modalAlert').classList.add('d-none');
}

async function openEditModal(id) {
    var res = await apiCall(API_BASE + '/project/' + id);
    if (!res || !res.success) return;
    var p = res.data;

    document.getElementById('modalTitle').textContent = 'Edit Project';
    document.getElementById('saveTxt').textContent    = 'Update';
    document.getElementById('editId').value    = p.id;
    document.getElementById('pName').value     = p.name;
    document.getElementById('pDesc').value     = p.description || '';
    document.getElementById('pStatus').value   = p.status;
    document.getElementById('pPriority').value = p.priority;
    document.getElementById('pDueDate').value  = p.dueDate ? p.dueDate.substring(0, 10) : '';
    document.getElementById('modalAlert').classList.add('d-none');

    projectModal.show();
}

async function saveProject() {
    var id   = document.getElementById('editId').value;
    var name = document.getElementById('pName').value.trim();
    if (!name) { showModalAlert('Project name is required.', 'danger'); return; }

    var payload = {
        name:        name,
        description: document.getElementById('pDesc').value.trim() || null,
        status:      document.getElementById('pStatus').value,
        priority:    document.getElementById('pPriority').value,
        dueDate:     document.getElementById('pDueDate').value || null
    };

    var url    = id ? API_BASE + '/project/' + id : API_BASE + '/project';
    var method = id ? 'PUT' : 'POST';
    var res    = await apiCall(url, method, payload);
    if (!res) return;

    if (res.success) { projectModal.hide(); loadProjects(); }
    else showModalAlert(res.message || 'Save failed.', 'danger');
}

// ─── Delete ──────────────────────────────────────
function openDeleteModal(id, name) {
    deleteId = id;
    document.getElementById('delName').textContent = name;
    deleteModal.show();
}

async function confirmDelete() {
    if (!deleteId) return;
    var res = await apiCall(API_BASE + '/project/' + deleteId, 'DELETE');
    if (res && res.success) { deleteModal.hide(); loadProjects(); }
}

// ─── Filters ─────────────────────────────────────
function clearFilters() {
    document.getElementById('filterStatus').value   = '';
    document.getElementById('filterPriority').value = '';
    document.getElementById('searchInput').value    = '';
    loadProjects();
}

function showModalAlert(msg, type) {
    var box = document.getElementById('modalAlert');
    box.className = 'alert alert-' + type + ' mb-3';
    box.textContent = msg;
}

// ─── Search debounce ─────────────────────────────
document.getElementById('searchInput').addEventListener('input', debounce(loadProjects, 300));

loadProjects();
