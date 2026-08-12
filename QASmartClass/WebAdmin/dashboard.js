/**
 * QA SmartClass — Web Admin Dashboard
 * Phase 6A: Read heartbeat JSON files from SharedFiles directory
 * 
 * Data source priority:
 * 1. Local heartbeat_*.json files (from StatusReportService)
 * 2. API endpoint (when production backend is ready)
 * 3. Mock fallback (for demo/testing)
 */

const HEARTBEAT_DIR = '../SharedFiles/'; // Relative path to SharedFiles
const OFFLINE_THRESHOLD_MS = 10 * 60 * 1000; // 10 minutes
const AUTO_REFRESH_MS = 30 * 1000; // 30 seconds

let machineData = [];

// ═══════════════════════════════════════════════
//  DATA LOADING
// ═══════════════════════════════════════════════

async function loadData() {
    try {
        // Try API endpoint first (for production)
        // const response = await fetch('/api/heartbeat');
        // machineData = await response.json();

        // For local file-based mode, scan known heartbeat files
        const localData = await loadLocalHeartbeats();
        
        if (localData.length > 0) {
            machineData = localData;
            document.getElementById('connectionStatus').textContent = `${localData.length} máy | File mode`;
        } else {
            // Fallback to mock data for demo
            machineData = getMockData();
            document.getElementById('connectionStatus').textContent = 'Demo mode (mock data)';
        }

        renderDashboard();
        renderReport();
        updateKPI();
        document.getElementById('lastUpdate').textContent = new Date().toLocaleString('vi-VN');
    } catch (err) {
        console.error('Load data error:', err);
        machineData = getMockData();
        renderDashboard();
        renderReport();
        updateKPI();
        document.getElementById('connectionStatus').textContent = 'Demo mode';
    }
}

async function loadLocalHeartbeats() {
    const results = [];
    
    // Try to load individual heartbeat files
    // In a real deployment, a simple Node.js/Python server would list files
    // For client-side only, we try known machine names
    const knownMachines = getKnownMachines();
    
    for (const machineName of knownMachines) {
        try {
            const url = `${HEARTBEAT_DIR}heartbeat_${machineName}.json?t=${Date.now()}`;
            const response = await fetch(url);
            if (response.ok) {
                const data = await response.json();
                
                // Determine online/offline based on timestamp
                const lastSeen = new Date(data.timestamp);
                const isOnline = (Date.now() - lastSeen.getTime()) < OFFLINE_THRESHOLD_MS;
                data.status = isOnline ? 'Online' : 'Offline';
                
                results.push(data);
            }
        } catch {
            // File doesn't exist for this machine — skip
        }
    }

    return results;
}

function getKnownMachines() {
    // In production, this list would come from a config file or API
    // For now, try the local machine name + common lab names
    const machines = [];
    
    // Try to detect from localStorage (admin can configure)
    const stored = localStorage.getItem('qa_known_machines');
    if (stored) {
        try { return JSON.parse(stored); } catch {}
    }
    
    // Default: try current hostname + common patterns
    machines.push(location.hostname || 'localhost');
    for (let i = 1; i <= 50; i++) {
        machines.push(`LAB01-PC${String(i).padStart(2, '0')}`);
    }
    return machines;
}

function getMockData() {
    const now = new Date();
    return [
        { machineId: 'LAB01-PC01', status: 'Online', currentMode: 'Teacher', uptimeSeconds: 7200, timestamp: now.toISOString() },
        { machineId: 'LAB01-PC02', status: 'Online', currentMode: 'Student', uptimeSeconds: 6800, timestamp: now.toISOString() },
        { machineId: 'LAB01-PC03', status: 'Online', currentMode: 'Student', uptimeSeconds: 5400, timestamp: new Date(now - 120000).toISOString() },
        { machineId: 'LAB01-PC04', status: 'Offline', currentMode: 'Unknown', uptimeSeconds: 0, timestamp: new Date(now - 3600000).toISOString() },
        { machineId: 'LAB01-PC05', status: 'Online', currentMode: 'SmartTouch', uptimeSeconds: 4200, timestamp: now.toISOString() },
    ];
}

// ═══════════════════════════════════════════════
//  KPI RENDERING
// ═══════════════════════════════════════════════

function updateKPI() {
    const total = machineData.length;
    const online = machineData.filter(m => m.status === 'Online').length;
    const offline = total - online;
    const avgUptime = online > 0
        ? machineData.filter(m => m.status === 'Online').reduce((sum, m) => sum + (m.uptimeSeconds || 0), 0) / online
        : 0;

    document.getElementById('kpiTotal').textContent = total;
    document.getElementById('kpiOnline').textContent = online;
    document.getElementById('kpiOffline').textContent = offline;
    document.getElementById('kpiUptime').textContent = formatUptime(avgUptime);
}

// ═══════════════════════════════════════════════
//  MACHINE GRID
// ═══════════════════════════════════════════════

function renderDashboard() {
    const grid = document.getElementById('machineGrid');
    grid.innerHTML = '';

    if (machineData.length === 0) {
        grid.innerHTML = '<div class="empty-state"><div class="icon">📡</div><p>Chưa có máy nào được phát hiện.<br>Hãy mở ứng dụng QA SmartClass trên các máy tính.</p></div>';
        return;
    }

    // Sort: online first, then by machineId
    const sorted = [...machineData].sort((a, b) => {
        if (a.status !== b.status) return a.status === 'Online' ? -1 : 1;
        return (a.machineId || '').localeCompare(b.machineId || '');
    });

    sorted.forEach(machine => {
        const isOnline = machine.status === 'Online';
        const card = document.createElement('div');
        card.className = `card ${isOnline ? 'online' : 'offline'}`;

        const uptimeStr = isOnline ? formatUptime(machine.uptimeSeconds || 0) : '---';
        const lastSeenStr = machine.timestamp ? new Date(machine.timestamp).toLocaleString('vi-VN') : '---';
        const modeIcon = getModeIcon(machine.currentMode);

        card.innerHTML = `
            <div class="card-header">
                <span class="machine-id">💻 ${machine.machineId || 'Unknown'}</span>
                <span class="status-badge ${isOnline ? 'online' : 'offline'}">
                    ${isOnline ? '🟢' : '🔴'} ${machine.status}
                </span>
            </div>
            <div class="info-row"><span>Chế độ:</span><span class="val">${modeIcon} ${machine.currentMode || 'N/A'}</span></div>
            <div class="info-row"><span>Uptime:</span><span class="val">${uptimeStr}</span></div>
            <div class="info-row"><span>Báo cáo cuối:</span><span class="val">${lastSeenStr}</span></div>
        `;

        grid.appendChild(card);
    });
}

// ═══════════════════════════════════════════════
//  REPORT TABLE
// ═══════════════════════════════════════════════

function renderReport() {
    const tbody = document.getElementById('reportBody');
    tbody.innerHTML = '';

    machineData.forEach(machine => {
        const isOnline = machine.status === 'Online';
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td><strong>${machine.machineId || 'Unknown'}</strong></td>
            <td><span class="status-badge ${isOnline ? 'online' : 'offline'}">${machine.status}</span></td>
            <td>${getModeIcon(machine.currentMode)} ${machine.currentMode || 'N/A'}</td>
            <td>${isOnline ? formatUptime(machine.uptimeSeconds || 0) : '---'}</td>
            <td>${machine.timestamp ? new Date(machine.timestamp).toLocaleString('vi-VN') : '---'}</td>
        `;
        tbody.appendChild(tr);
    });
}

// ═══════════════════════════════════════════════
//  HELPERS
// ═══════════════════════════════════════════════

function formatUptime(seconds) {
    if (!seconds || seconds <= 0) return '0m';
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    if (hours > 0) return `${hours}h ${minutes}m`;
    return `${minutes}m`;
}

function getModeIcon(mode) {
    switch (mode) {
        case 'Teacher': return '👨‍🏫';
        case 'Student': return '🎓';
        case 'SmartTouch': return '🖊️';
        default: return '❓';
    }
}

// ═══════════════════════════════════════════════
//  ADMIN CONFIG
// ═══════════════════════════════════════════════

function configureMachines() {
    const input = prompt('Nhập danh sách máy (cách nhau bằng dấu phẩy):\nVD: LAB01-PC01,LAB01-PC02,TEACHER-PC');
    if (input) {
        const machines = input.split(',').map(m => m.trim()).filter(m => m.length > 0);
        localStorage.setItem('qa_known_machines', JSON.stringify(machines));
        loadData();
    }
}

// ═══════════════════════════════════════════════
//  INITIALIZATION
// ═══════════════════════════════════════════════

loadData();
setInterval(loadData, AUTO_REFRESH_MS);
