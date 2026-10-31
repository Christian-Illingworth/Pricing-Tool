/* Consultiv Utilities web design kit: behaviour for the components in components.css.
   No dependencies. Load with <script src="js/kit.js" defer></script>; everything wires itself up
   from data attributes on DOMContentLoaded, and the same functions are on window.CU for scripts.

   data-tab="id"            on a .nav-item: shows #pane-id (.tab-pane), sets the top bar title
                            from data-title. A pane with data-flush fills the content edge to edge.
   .split                   clicking a .list-row selects it; .detail-back returns to the list.
   .nav-group               toggles the .nav-children right after it.
   data-menu="menuId"       on a trigger: opens/closes that .menu. Items with role="menuitemradio"
                            become the selected value and the trigger's [data-label] shows it.
   data-open="dialogId"     opens a dialog (a .modal-overlay or a .drawer-wrap, starting [hidden]).
   data-close               inside a dialog: closes it. So do Escape and a click on the backdrop.
   CU.toast(msg, opts)      shows a toast. opts: { error, action: { label, onClick }, duration }.
   CU.donut(el, data, opts) draws a donut chart. data: [{ label, value, tone }] where tone is a
                            colour token name ('teal', 'cyan', 'coral'…). opts: { label, sub }. */
(function () {
  'use strict';

  var RAIL_BELOW = 1100;  // px: the sidebar is an icon rail below this width
  var STORE_KEY = 'cu.sidebarCollapsed';

  function store(key, value) {
    try {
      if (value === undefined) return localStorage.getItem(key);
      localStorage.setItem(key, value);
    } catch (e) { return null; }  // private windows can throw; the kit works without it
  }

  // ── Sidebar ──
  function initSidebar() {
    var shell = document.querySelector('.app-shell');
    if (!shell) return;
    var narrow = window.matchMedia('(max-width: ' + (RAIL_BELOW - 1) + 'px)');
    var expand = shell.querySelector('.sidebar-expand');

    function apply() {
      var rail = narrow.matches || store(STORE_KEY) === '1';
      shell.classList.toggle('is-rail', rail);
      if (!rail) closeFlyout();
    }
    function closeFlyout() {
      shell.classList.remove('sidebar-open');
      if (expand) expand.setAttribute('aria-expanded', 'false');
    }
    function toggle() {
      if (narrow.matches) {
        var open = !shell.classList.contains('sidebar-open');
        shell.classList.toggle('sidebar-open', open);
        if (expand) expand.setAttribute('aria-expanded', String(open));
      } else {
        store(STORE_KEY, shell.classList.contains('is-rail') ? '0' : '1');
        apply();
      }
    }

    shell.querySelectorAll('.sidebar-collapse, .sidebar-expand').forEach(function (b) {
      b.addEventListener('click', toggle);
    });
    // A slid-out sidebar closes on a click outside it, on Escape, or once you pick a page.
    document.addEventListener('click', function (e) {
      if (shell.classList.contains('sidebar-open') && !e.target.closest('.sidebar')) closeFlyout();
    });
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && shell.classList.contains('sidebar-open')) { closeFlyout(); if (expand) expand.focus(); }
    });
    shell.querySelectorAll('.sidebar .nav-item[data-tab]').forEach(function (b) {
      b.addEventListener('click', function () { if (narrow.matches) closeFlyout(); });
    });
    narrow.addEventListener('change', apply);
    apply();
  }

  function initNavGroups() {
    document.querySelectorAll('.nav-group').forEach(function (g) {
      g.addEventListener('click', function () {
        var open = g.getAttribute('aria-expanded') !== 'true';
        g.setAttribute('aria-expanded', String(open));
        var kids = g.nextElementSibling;
        if (kids && kids.classList.contains('nav-children')) kids.hidden = !open;
      });
    });
  }

  // ── Tabs ──
  function showTab(id) {
    var pane = document.getElementById('pane-' + id);
    if (!pane) return;
    document.querySelectorAll('.tab-pane.active').forEach(function (p) { p.classList.remove('active'); });
    pane.classList.add('active');
    document.querySelectorAll('.nav-item[data-tab]').forEach(function (n) {
      var on = n.getAttribute('data-tab') === id;
      n.classList.toggle('active', on);
      if (on) n.setAttribute('aria-current', 'page'); else n.removeAttribute('aria-current');
      if (on && n.dataset.title) {
        var title = document.querySelector('.topbar-title');
        if (title) title.textContent = n.dataset.title;
      }
    });
    var content = pane.closest('.content');
    if (content) {
      content.scrollTop = 0;
      // A pane marked data-flush (a split view) fills the content edge to edge.
      content.classList.toggle('content--flush', pane.hasAttribute('data-flush'));
    }
  }
  function initTabs() {
    document.querySelectorAll('.nav-item[data-tab]').forEach(function (n) {
      n.addEventListener('click', function () { showTab(n.getAttribute('data-tab')); });
    });
  }

  // ── Split view ── picking a row selects it and, below 1100px, swaps the list for the detail.
  function initSplits() {
    document.querySelectorAll('.split').forEach(function (split) {
      split.addEventListener('click', function (e) {
        var row = e.target.closest('.split-list .list-row');
        if (row) {
          split.querySelectorAll('.list-row.is-selected').forEach(function (r) { r.classList.remove('is-selected'); r.removeAttribute('aria-current'); });
          row.classList.add('is-selected');
          row.setAttribute('aria-current', 'true');
          split.classList.add('is-detail-open');
          return;
        }
        if (e.target.closest('.detail-back')) {
          split.classList.remove('is-detail-open');
          var sel = split.querySelector('.list-row.is-selected');
          if (sel) sel.focus();
        }
      });
    });
  }

  // ── Menus ──
  var openMenu = null;  // { menu, trigger }
  function closeMenu(refocus) {
    if (!openMenu) return;
    openMenu.menu.hidden = true;
    openMenu.trigger.setAttribute('aria-expanded', 'false');
    if (refocus) openMenu.trigger.focus();
    openMenu = null;
  }
  function initMenus() {
    document.querySelectorAll('[data-menu]').forEach(function (trigger) {
      var menu = document.getElementById(trigger.getAttribute('data-menu'));
      if (!menu) return;
      trigger.setAttribute('aria-haspopup', 'menu');
      trigger.setAttribute('aria-expanded', 'false');
      trigger.addEventListener('click', function (e) {
        e.stopPropagation();
        var wasOpen = openMenu && openMenu.menu === menu;
        closeMenu(false);
        if (wasOpen) return;
        menu.hidden = false;
        trigger.setAttribute('aria-expanded', 'true');
        openMenu = { menu: menu, trigger: trigger };
        var first = menu.querySelector('[aria-checked="true"]') || menu.querySelector('.menu-item');
        if (first) first.focus();
      });
      menu.addEventListener('click', function (e) {
        var item = e.target.closest('.menu-item');
        if (!item) return;
        if (item.getAttribute('role') === 'menuitemradio') {
          menu.querySelectorAll('[role="menuitemradio"]').forEach(function (i) { i.setAttribute('aria-checked', String(i === item)); });
          var label = trigger.querySelector('[data-label]');
          if (label) label.textContent = item.dataset.value || item.textContent.trim();
          // Filtering by anything but the default gets the trigger's teal "filtered" look.
          trigger.classList.toggle('is-filtered', !item.hasAttribute('data-default'));
        }
        closeMenu(true);
      });
      // Up and down move between items while the menu is open.
      menu.addEventListener('keydown', function (e) {
        if (e.key !== 'ArrowDown' && e.key !== 'ArrowUp') return;
        e.preventDefault();
        var items = Array.prototype.slice.call(menu.querySelectorAll('.menu-item'));
        var i = items.indexOf(document.activeElement);
        var next = items[(i + (e.key === 'ArrowDown' ? 1 : items.length - 1)) % items.length];
        if (next) next.focus();
      });
    });
    document.addEventListener('click', function (e) {
      if (openMenu && !openMenu.menu.contains(e.target)) closeMenu(false);
    });
  }

  // ── Dialogs (modals and drawers) ──
  var dialogStack = [];  // [{ el, returnTo }]
  function openDialog(id) {
    var el = typeof id === 'string' ? document.getElementById(id) : id;
    if (!el || !el.hidden) return;
    dialogStack.push({ el: el, returnTo: document.activeElement });
    el.hidden = false;
    // a[href], not [href]: an icon's <svg><use href> matches [href] but can't take focus.
    var focusable = el.querySelector('[autofocus], input, select, textarea, button:not([data-close]), a[href], [tabindex]:not([tabindex="-1"])') || el.querySelector('[data-close]');
    if (focusable) focusable.focus();
  }
  function closeDialog(id) {
    var el = typeof id === 'string' ? document.getElementById(id) : id;
    if (!el || el.hidden) return;
    var entry = null;
    dialogStack = dialogStack.filter(function (d) { if (d.el === el) { entry = d; return false; } return true; });
    var finish = function () {
      el.hidden = true;
      el.classList.remove('closing');
      if (entry && entry.returnTo && entry.returnTo.focus) entry.returnTo.focus();
    };
    // A modal plays its exit first; reduced motion skips it.
    if (el.classList.contains('modal-overlay') && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      el.classList.add('closing');
      setTimeout(finish, 160);
    } else finish();
  }
  function initDialogs() {
    document.addEventListener('click', function (e) {
      var opener = e.target.closest('[data-open]');
      if (opener) { openDialog(opener.getAttribute('data-open')); return; }
      var closer = e.target.closest('[data-close]');
      if (closer) { closeDialog(closer.closest('.modal-overlay, .drawer-wrap')); return; }
      // A click on the backdrop itself, not on the panel.
      if (e.target.classList && e.target.classList.contains('modal-overlay')) closeDialog(e.target);
    });
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && dialogStack.length && !openMenu) closeDialog(dialogStack[dialogStack.length - 1].el);
      if (e.key === 'Escape' && openMenu) closeMenu(true);
    });
  }

  // ── Toasts ──
  var CLOSE_ICON = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" aria-hidden="true"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>';
  function toast(message, opts) {
    opts = opts || {};
    var stack = document.querySelector('.toast-stack');
    if (!stack) {
      stack = document.createElement('div');
      stack.className = 'toast-stack';
      document.body.appendChild(stack);
    }
    var el = document.createElement('div');
    el.className = 'toast';
    el.setAttribute('role', opts.error ? 'alert' : 'status');
    if (opts.error) el.insertAdjacentHTML('beforeend', '<span class="toast-mark" aria-hidden="true"></span>');
    var text = document.createElement('span');
    text.className = 'toast-text';
    text.textContent = message;
    el.appendChild(text);

    var timer = null;
    var dismiss = function () {
      clearTimeout(timer);
      el.classList.remove('is-shown');
      setTimeout(function () { el.remove(); }, 250);
    };
    if (opts.action) {
      var act = document.createElement('button');
      act.type = 'button';
      act.className = 'toast-action';
      act.textContent = opts.action.label;
      act.addEventListener('click', function () { dismiss(); if (opts.action.onClick) opts.action.onClick(); });
      el.appendChild(act);
    }
    if (opts.action || opts.error) {
      var close = document.createElement('button');
      close.type = 'button';
      close.className = 'toast-close';
      close.setAttribute('aria-label', 'Dismiss');
      close.innerHTML = CLOSE_ICON;
      close.addEventListener('click', dismiss);
      el.appendChild(close);
    }
    stack.appendChild(el);
    requestAnimationFrame(function () { requestAnimationFrame(function () { el.classList.add('is-shown'); }); });
    // Errors and toasts with an action stay until dismissed, unless told otherwise.
    var duration = opts.duration != null ? opts.duration : (opts.error || opts.action ? 0 : 4000);
    if (duration) timer = setTimeout(dismiss, duration);
    return { dismiss: dismiss };
  }

  // ── Donut ──
  var SVG_NS = 'http://www.w3.org/2000/svg';
  function donut(el, data, opts) {
    opts = opts || {};
    var total = data.reduce(function (s, d) { return s + d.value; }, 0);
    var R = 80, C = 2 * Math.PI * R, GAP = data.length > 1 ? 2 : 0;
    el.classList.add('donut');
    el.innerHTML = '';

    var body = document.createElement('div');
    body.className = 'donut-body';
    var ring = document.createElement('div');
    ring.className = 'donut-ring';
    var svg = document.createElementNS(SVG_NS, 'svg');
    svg.setAttribute('viewBox', '0 0 200 200');
    svg.setAttribute('aria-hidden', 'true');
    var track = document.createElementNS(SVG_NS, 'circle');
    track.setAttribute('class', 'donut-track');
    track.setAttribute('cx', 100); track.setAttribute('cy', 100); track.setAttribute('r', R);
    svg.appendChild(track);

    var centre = document.createElement('div');
    centre.className = 'donut-centre';
    centre.innerHTML = '<span class="donut-c-label"></span><span class="donut-c-num"></span><span class="donut-c-sub"></span>';
    var cLabel = centre.children[0], cNum = centre.children[1], cSub = centre.children[2];

    var side = document.createElement('div');
    side.className = 'donut-side';
    var legend = document.createElement('ul');
    legend.className = 'donut-legend';

    var segs = [], rows = [], start = 0;
    data.forEach(function (d, i) {
      var colour = 'var(--' + (d.tone || 'series-' + ((i % 10) + 1)) + ')';
      var len = total ? (d.value / total) * C : 0;
      var seg = document.createElementNS(SVG_NS, 'circle');
      seg.setAttribute('class', 'donut-seg');
      seg.setAttribute('cx', 100); seg.setAttribute('cy', 100); seg.setAttribute('r', R);
      seg.style.stroke = colour;
      seg.setAttribute('stroke-dasharray', Math.max(len - GAP, 0) + ' ' + (C - Math.max(len - GAP, 0)));
      seg.setAttribute('stroke-dashoffset', String(-start));
      start += len;
      svg.appendChild(seg);
      segs.push(seg);

      var pct = total ? Math.round((d.value / total) * 100) : 0;
      var row = document.createElement('li');
      row.className = 'donut-row';
      row.tabIndex = 0;
      row.innerHTML = '<span class="dot"></span><span class="donut-name"></span><span class="donut-pct"></span><span class="donut-count"></span>';
      row.children[0].style.setProperty('--c', colour);
      row.children[1].textContent = d.label;
      row.children[2].textContent = pct + '%';
      row.children[3].textContent = d.value.toLocaleString();
      if (d.onClick) { row.addEventListener('click', d.onClick); seg.addEventListener('click', d.onClick); }
      legend.appendChild(row);
      rows.push(row);
    });

    function focus(i) {
      ring.classList.toggle('is-focus', i != null);
      segs.forEach(function (s, j) { s.classList.toggle('is-on', j === i); });
      rows.forEach(function (r, j) { r.classList.toggle('is-on', j === i); });
      if (i == null) {
        cLabel.textContent = opts.label || 'Total';
        cNum.textContent = total.toLocaleString();
        cSub.textContent = opts.sub || '';
      } else {
        var d = data[i];
        cLabel.textContent = d.label;
        cNum.textContent = d.value.toLocaleString();
        cSub.textContent = (total ? Math.round((d.value / total) * 100) : 0) + '% of ' + total.toLocaleString();
      }
    }
    segs.forEach(function (s, i) { s.addEventListener('mouseenter', function () { focus(i); }); });
    rows.forEach(function (r, i) {
      r.addEventListener('mouseenter', function () { focus(i); });
      r.addEventListener('focus', function () { focus(i); });
      r.addEventListener('blur', function () { focus(null); });
    });
    body.addEventListener('mouseleave', function () { focus(null); });

    ring.appendChild(svg);
    ring.appendChild(centre);
    side.appendChild(legend);
    body.appendChild(ring);
    body.appendChild(side);
    el.appendChild(body);
    focus(null);
    return { focus: focus };
  }

  window.CU = { toast: toast, donut: donut, showTab: showTab, openDialog: openDialog, closeDialog: closeDialog, closeMenu: closeMenu };

  function init() {
    initSidebar();
    initNavGroups();
    initTabs();
    initSplits();
    initMenus();
    initDialogs();
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
