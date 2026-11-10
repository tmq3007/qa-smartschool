(() => {
  'use strict';
  const q = new URLSearchParams(location.hash.slice(1));
  const id = q.get('id');
  const token = q.get('token') || '0123456789abcdef0123456789abcdef';

  if (!id || !/^[a-z0-9-]+$/.test(id)) {
    const st = document.getElementById('playerStatus');
    if (st) st.textContent = 'Dữ liệu mở mô phỏng không hợp lệ.';
    return;
  }

  function sendInit() {
    try {
      window.postMessage({
        nbPhysics: 1,
        token: token,
        type: 'init',
        scene: { id: id, states: {}, refs: {} }
      }, '*');
      const st = document.getElementById('playerStatus');
      if (st) st.style.display = 'none';
    } catch (e) {
      console.error('sendInit error:', e);
    }
  }

  // Tự động gửi init khi engine (flat.js / space.js) báo ready
  window.addEventListener('message', (e) => {
    if (!e.data || e.data.nbPhysics !== 1) return;
    if (e.data.type === 'ready') {
      sendInit();
    }
    // Đồng bộ trạng thái chạy / dừng sang C# WebView2
    if (e.data.type === 'playback' || e.data.type === 'state') {
      try {
        if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage({
            type: 'playback',
            running: !!e.data.running,
            canPlay: e.data.canPlay !== false
          });
        }
      } catch (_) {}
    }
  });

  // Tự động reload khi đổi mô phỏng qua hash
  window.addEventListener('hashchange', () => {
    location.reload();
  });

  // Gửi bổ trợ sau khi script mount để chắc chắn nhận được
  setTimeout(sendInit, 200);
  setTimeout(sendInit, 600);
  setTimeout(sendInit, 1200);

  const spaceIds = ["projectile", "free-fall", "circuit", "magnetic-field", "magnetic-force", "em-induction", "ac-generator", "nucleus"];
  const scriptName = spaceIds.includes(id) ? 'space.js' : 'flat.js';
  const s = document.createElement('script');
  s.src = scriptName;
  s.onerror = () => {
    const st = document.getElementById('playerStatus');
    if (st) st.textContent = 'Không tải được thành phần mô phỏng: ' + scriptName;
  };
  document.body.append(s);
})();
