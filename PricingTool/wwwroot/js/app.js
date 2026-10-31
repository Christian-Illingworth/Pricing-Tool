/* CU Pricing Tool: small progressive enhancements on top of the design kit's kit.js.
   Every flow also works as plain links and form posts with script turned off. There are no inline
   scripts anywhere (the CSP allows script-src 'self' only), so all wiring is here, delegated from
   the document so it also works for HTML swapped in later.

   data-swap="#targetId"  on a form or link: fetch it and put the returned fragment into the target.
   data-auto-continue     on a button: clicked once the page has loaded (the sign-in loading page).

   window.PT.request(url, options)    fetch with the antiforgery token and the fetch marker header.
   window.PT.swapFragment(el, html)   put server-rendered HTML into el (refuses kit-bound markup). */
(function () {
  'use strict';

  // ── Requests ──

  function antiforgeryToken() {
    var input = document.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
  }

  // Same-origin fetch that sends the antiforgery token as the RequestVerificationToken header, and
  // X-Requested-With so the server answers a lapsed session with 401/403 rather than a redirect.
  function request(url, options) {
    options = options || {};
    var headers = new Headers(options.headers || {});
    headers.set('RequestVerificationToken', antiforgeryToken());
    headers.set('X-Requested-With', 'XMLHttpRequest');
    var init = {};
    Object.keys(options).forEach(function (key) { init[key] = options[key]; });
    init.headers = headers;
    init.credentials = 'same-origin';
    return fetch(url, init).then(function (response) {
      if (response.status === 401) { window.location.reload(); }
      return response;
    });
  }

  // ── Swapping fragments ──

  // kit.js binds menus, tabs and split views once per element at page load and has no re-initialise
  // function (research R24, kit gap 1), so a fragment containing them would silently not work.
  var KIT_BOUND = '[data-menu], [data-tab], .split';

  function swapFragment(target, html) {
    var template = document.createElement('template');
    template.innerHTML = html;
    if (template.content.querySelector(KIT_BOUND)) {
      throw new Error('Refusing to swap in HTML that contains data-menu, data-tab or .split.');
    }
    target.replaceChildren(template.content);
  }

  function swapFrom(response, target) {
    if (!response.ok) { throw new Error('The server answered ' + response.status + '.'); }
    return response.text().then(function (html) { swapFragment(target, html); });
  }

  function failed(error) {
    if (window.CU && window.CU.toast) {
      window.CU.toast("That didn't work. Try again, or reload the page.", { error: true });
    }
    if (window.console) { window.console.error(error); }
  }

  function targetOf(el) {
    var selector = el.getAttribute('data-swap');
    return selector ? document.querySelector(selector) : null;
  }

  // ── Delegated handlers ──

  document.addEventListener('submit', function (e) {
    var form = e.target;
    if (!(form instanceof HTMLFormElement) || !form.hasAttribute('data-swap')) { return; }
    var target = targetOf(form);
    if (!target) { return; }
    e.preventDefault();

    var method = (form.getAttribute('method') || 'get').toUpperCase();
    var data = new FormData(form);
    if (e.submitter && e.submitter.name) { data.append(e.submitter.name, e.submitter.value); }
    var url = form.action;
    var options = { method: method };
    if (method === 'GET') {
      url += (url.indexOf('?') === -1 ? '?' : '&') + new URLSearchParams(data).toString();
    } else {
      options.body = data;
    }
    target.setAttribute('aria-busy', 'true');
    request(url, options)
      .then(function (response) { return swapFrom(response, target); })
      .catch(failed)
      .finally(function () { target.removeAttribute('aria-busy'); });
  });

  document.addEventListener('click', function (e) {
    var link = e.target instanceof Element ? e.target.closest('a[data-swap]') : null;
    if (!link || e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0) { return; }
    var target = targetOf(link);
    if (!target) { return; }
    e.preventDefault();
    target.setAttribute('aria-busy', 'true');
    request(link.href, { method: 'GET' })
      .then(function (response) { return swapFrom(response, target); })
      .catch(failed)
      .finally(function () { target.removeAttribute('aria-busy'); });
  });

  function autoContinue() {
    var button = document.querySelector('[data-auto-continue]');
    if (button) { button.click(); }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', autoContinue);
  } else {
    autoContinue();
  }

  window.PT = { request: request, swapFragment: swapFragment };
})();
