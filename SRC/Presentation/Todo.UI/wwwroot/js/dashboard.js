requireAuth();

document.getElementById('todayDate').textContent = new Date().toLocaleDateString('en-GB', {
    weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
});

var barInst = null;
var pieInst = null;

async function loadStats() {
    var res = await apiCall(API_BASE + '/dashboard/stats');
    if (!res || !res.success) return;
    var d = res.data;

    document.getElementById('kpiProjects').textContent = d.totalProjects;
    document.getElementById('kpiPending').textContent  = d.pendingTasks;
    document.getElementById('kpiHigh').textContent     = d.highPriorityTasks;
    document.getElementById('kpiDone').textContent     = d.completedTasks;

    renderBar(d.projectTaskChart);
    renderPie(d.taskStatusChart);
}

function chartDefaults() {
    var dark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
    return {
        gridColor:  dark ? 'rgba(255,255,255,.06)' : 'rgba(0,0,0,.05)',
        tickColor:  dark ? '#64748b' : '#94a3b8',
        legendColor: dark ? '#94a3b8' : '#64748b'
    };
}

function renderBar(data) {
    var ctx = document.getElementById('barChart').getContext('2d');
    if (barInst) barInst.destroy();
    var c = chartDefaults();

    barInst = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.map(function(x) { return x.projectName; }),
            datasets: [{
                label: 'Tasks',
                data: data.map(function(x) { return x.taskCount; }),
                backgroundColor: 'rgba(67,97,238,.75)',
                hoverBackgroundColor: '#4361ee',
                borderRadius: 6,
                borderSkipped: false
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: { precision: 0, color: c.tickColor, font: { size: 11 } },
                    grid: { color: c.gridColor }
                },
                x: {
                    ticks: { color: c.tickColor, font: { size: 11 } },
                    grid: { display: false }
                }
            }
        }
    });
}

function renderPie(data) {
    var ctx = document.getElementById('pieChart').getContext('2d');
    if (pieInst) pieInst.destroy();
    var c = chartDefaults();

    var palette = { Pending: '#f59e0b', InProgress: '#4361ee', Completed: '#10b981' };

    pieInst = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: data.map(function(x) { return x.status; }),
            datasets: [{
                data: data.map(function(x) { return x.count; }),
                backgroundColor: data.map(function(x) { return palette[x.status] || '#94a3b8'; }),
                borderWidth: 0,
                hoverOffset: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '70%',
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        color: c.legendColor,
                        padding: 14,
                        font: { size: 11 },
                        usePointStyle: true, pointStyleWidth: 8
                    }
                }
            }
        }
    });
}

loadStats();
