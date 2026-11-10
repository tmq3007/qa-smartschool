"use strict";
(() => {
  var __create = Object.create;
  var __defProp = Object.defineProperty;
  var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
  var __getOwnPropNames = Object.getOwnPropertyNames;
  var __getProtoOf = Object.getPrototypeOf;
  var __hasOwnProp = Object.prototype.hasOwnProperty;
  var __defNormalProp = (obj, key, value) => key in obj ? __defProp(obj, key, { enumerable: true, configurable: true, writable: true, value }) : obj[key] = value;
  var __commonJS = (cb, mod) => function __require() {
    return mod || (0, cb[__getOwnPropNames(cb)[0]])((mod = { exports: {} }).exports, mod), mod.exports;
  };
  var __copyProps = (to, from, except, desc) => {
    if (from && typeof from === "object" || typeof from === "function") {
      for (let key of __getOwnPropNames(from))
        if (!__hasOwnProp.call(to, key) && key !== except)
          __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
    }
    return to;
  };
  var __toESM = (mod, isNodeMode, target) => (target = mod != null ? __create(__getProtoOf(mod)) : {}, __copyProps(
    // If the importer is in node compatibility mode or this is not an ESM
    // file that has been converted to a CommonJS file using a Babel-
    // compatible transform (i.e. "__esModule" has not been set), then set
    // "default" to the CommonJS "module.exports" for node compatibility.
    isNodeMode || !mod || !mod.__esModule ? __defProp(target, "default", { value: mod, enumerable: true }) : target,
    mod
  ));
  var __publicField = (obj, key, value) => {
    __defNormalProp(obj, typeof key !== "symbol" ? key + "" : key, value);
    return value;
  };

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react/cjs/react.production.min.js
  var require_react_production_min = __commonJS({
    "../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react/cjs/react.production.min.js"(exports) {
      "use strict";
      var l = Symbol.for("react.element");
      var n = Symbol.for("react.portal");
      var p = Symbol.for("react.fragment");
      var q = Symbol.for("react.strict_mode");
      var r = Symbol.for("react.profiler");
      var t = Symbol.for("react.provider");
      var u = Symbol.for("react.context");
      var v = Symbol.for("react.forward_ref");
      var w = Symbol.for("react.suspense");
      var x = Symbol.for("react.memo");
      var y = Symbol.for("react.lazy");
      var z = Symbol.iterator;
      function A(a) {
        if (null === a || "object" !== typeof a)
          return null;
        a = z && a[z] || a["@@iterator"];
        return "function" === typeof a ? a : null;
      }
      var B = { isMounted: function() {
        return false;
      }, enqueueForceUpdate: function() {
      }, enqueueReplaceState: function() {
      }, enqueueSetState: function() {
      } };
      var C3 = Object.assign;
      var D = {};
      function E(a, b, e) {
        this.props = a;
        this.context = b;
        this.refs = D;
        this.updater = e || B;
      }
      E.prototype.isReactComponent = {};
      E.prototype.setState = function(a, b) {
        if ("object" !== typeof a && "function" !== typeof a && null != a)
          throw Error("setState(...): takes an object of state variables to update or a function which returns an object of state variables.");
        this.updater.enqueueSetState(this, a, b, "setState");
      };
      E.prototype.forceUpdate = function(a) {
        this.updater.enqueueForceUpdate(this, a, "forceUpdate");
      };
      function F() {
      }
      F.prototype = E.prototype;
      function G4(a, b, e) {
        this.props = a;
        this.context = b;
        this.refs = D;
        this.updater = e || B;
      }
      var H12 = G4.prototype = new F();
      H12.constructor = G4;
      C3(H12, E.prototype);
      H12.isPureReactComponent = true;
      var I = Array.isArray;
      var J = Object.prototype.hasOwnProperty;
      var K = { current: null };
      var L = { key: true, ref: true, __self: true, __source: true };
      function M(a, b, e) {
        var d, c = {}, k = null, h = null;
        if (null != b)
          for (d in void 0 !== b.ref && (h = b.ref), void 0 !== b.key && (k = "" + b.key), b)
            J.call(b, d) && !L.hasOwnProperty(d) && (c[d] = b[d]);
        var g = arguments.length - 2;
        if (1 === g)
          c.children = e;
        else if (1 < g) {
          for (var f = Array(g), m = 0; m < g; m++)
            f[m] = arguments[m + 2];
          c.children = f;
        }
        if (a && a.defaultProps)
          for (d in g = a.defaultProps, g)
            void 0 === c[d] && (c[d] = g[d]);
        return { $$typeof: l, type: a, key: k, ref: h, props: c, _owner: K.current };
      }
      function N(a, b) {
        return { $$typeof: l, type: a.type, key: b, ref: a.ref, props: a.props, _owner: a._owner };
      }
      function O2(a) {
        return "object" === typeof a && null !== a && a.$$typeof === l;
      }
      function escape(a) {
        var b = { "=": "=0", ":": "=2" };
        return "$" + a.replace(/[=:]/g, function(a2) {
          return b[a2];
        });
      }
      var P = /\/+/g;
      function Q(a, b) {
        return "object" === typeof a && null !== a && null != a.key ? escape("" + a.key) : b.toString(36);
      }
      function R(a, b, e, d, c) {
        var k = typeof a;
        if ("undefined" === k || "boolean" === k)
          a = null;
        var h = false;
        if (null === a)
          h = true;
        else
          switch (k) {
            case "string":
            case "number":
              h = true;
              break;
            case "object":
              switch (a.$$typeof) {
                case l:
                case n:
                  h = true;
              }
          }
        if (h)
          return h = a, c = c(h), a = "" === d ? "." + Q(h, 0) : d, I(c) ? (e = "", null != a && (e = a.replace(P, "$&/") + "/"), R(c, b, e, "", function(a2) {
            return a2;
          })) : null != c && (O2(c) && (c = N(c, e + (!c.key || h && h.key === c.key ? "" : ("" + c.key).replace(P, "$&/") + "/") + a)), b.push(c)), 1;
        h = 0;
        d = "" === d ? "." : d + ":";
        if (I(a))
          for (var g = 0; g < a.length; g++) {
            k = a[g];
            var f = d + Q(k, g);
            h += R(k, b, e, f, c);
          }
        else if (f = A(a), "function" === typeof f)
          for (a = f.call(a), g = 0; !(k = a.next()).done; )
            k = k.value, f = d + Q(k, g++), h += R(k, b, e, f, c);
        else if ("object" === k)
          throw b = String(a), Error("Objects are not valid as a React child (found: " + ("[object Object]" === b ? "object with keys {" + Object.keys(a).join(", ") + "}" : b) + "). If you meant to render a collection of children, use an array instead.");
        return h;
      }
      function S2(a, b, e) {
        if (null == a)
          return a;
        var d = [], c = 0;
        R(a, d, "", "", function(a2) {
          return b.call(e, a2, c++);
        });
        return d;
      }
      function T2(a) {
        if (-1 === a._status) {
          var b = a._result;
          b = b();
          b.then(function(b2) {
            if (0 === a._status || -1 === a._status)
              a._status = 1, a._result = b2;
          }, function(b2) {
            if (0 === a._status || -1 === a._status)
              a._status = 2, a._result = b2;
          });
          -1 === a._status && (a._status = 0, a._result = b);
        }
        if (1 === a._status)
          return a._result.default;
        throw a._result;
      }
      var U = { current: null };
      var V = { transition: null };
      var W12 = { ReactCurrentDispatcher: U, ReactCurrentBatchConfig: V, ReactCurrentOwner: K };
      function X() {
        throw Error("act(...) is not supported in production builds of React.");
      }
      exports.Children = { map: S2, forEach: function(a, b, e) {
        S2(a, function() {
          b.apply(this, arguments);
        }, e);
      }, count: function(a) {
        var b = 0;
        S2(a, function() {
          b++;
        });
        return b;
      }, toArray: function(a) {
        return S2(a, function(a2) {
          return a2;
        }) || [];
      }, only: function(a) {
        if (!O2(a))
          throw Error("React.Children.only expected to receive a single React element child.");
        return a;
      } };
      exports.Component = E;
      exports.Fragment = p;
      exports.Profiler = r;
      exports.PureComponent = G4;
      exports.StrictMode = q;
      exports.Suspense = w;
      exports.__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED = W12;
      exports.act = X;
      exports.cloneElement = function(a, b, e) {
        if (null === a || void 0 === a)
          throw Error("React.cloneElement(...): The argument must be a React element, but you passed " + a + ".");
        var d = C3({}, a.props), c = a.key, k = a.ref, h = a._owner;
        if (null != b) {
          void 0 !== b.ref && (k = b.ref, h = K.current);
          void 0 !== b.key && (c = "" + b.key);
          if (a.type && a.type.defaultProps)
            var g = a.type.defaultProps;
          for (f in b)
            J.call(b, f) && !L.hasOwnProperty(f) && (d[f] = void 0 === b[f] && void 0 !== g ? g[f] : b[f]);
        }
        var f = arguments.length - 2;
        if (1 === f)
          d.children = e;
        else if (1 < f) {
          g = Array(f);
          for (var m = 0; m < f; m++)
            g[m] = arguments[m + 2];
          d.children = g;
        }
        return { $$typeof: l, type: a.type, key: c, ref: k, props: d, _owner: h };
      };
      exports.createContext = function(a) {
        a = { $$typeof: u, _currentValue: a, _currentValue2: a, _threadCount: 0, Provider: null, Consumer: null, _defaultValue: null, _globalName: null };
        a.Provider = { $$typeof: t, _context: a };
        return a.Consumer = a;
      };
      exports.createElement = M;
      exports.createFactory = function(a) {
        var b = M.bind(null, a);
        b.type = a;
        return b;
      };
      exports.createRef = function() {
        return { current: null };
      };
      exports.forwardRef = function(a) {
        return { $$typeof: v, render: a };
      };
      exports.isValidElement = O2;
      exports.lazy = function(a) {
        return { $$typeof: y, _payload: { _status: -1, _result: a }, _init: T2 };
      };
      exports.memo = function(a, b) {
        return { $$typeof: x, type: a, compare: void 0 === b ? null : b };
      };
      exports.startTransition = function(a) {
        var b = V.transition;
        V.transition = {};
        try {
          a();
        } finally {
          V.transition = b;
        }
      };
      exports.unstable_act = X;
      exports.useCallback = function(a, b) {
        return U.current.useCallback(a, b);
      };
      exports.useContext = function(a) {
        return U.current.useContext(a);
      };
      exports.useDebugValue = function() {
      };
      exports.useDeferredValue = function(a) {
        return U.current.useDeferredValue(a);
      };
      exports.useEffect = function(a, b) {
        return U.current.useEffect(a, b);
      };
      exports.useId = function() {
        return U.current.useId();
      };
      exports.useImperativeHandle = function(a, b, e) {
        return U.current.useImperativeHandle(a, b, e);
      };
      exports.useInsertionEffect = function(a, b) {
        return U.current.useInsertionEffect(a, b);
      };
      exports.useLayoutEffect = function(a, b) {
        return U.current.useLayoutEffect(a, b);
      };
      exports.useMemo = function(a, b) {
        return U.current.useMemo(a, b);
      };
      exports.useReducer = function(a, b, e) {
        return U.current.useReducer(a, b, e);
      };
      exports.useRef = function(a) {
        return U.current.useRef(a);
      };
      exports.useState = function(a) {
        return U.current.useState(a);
      };
      exports.useSyncExternalStore = function(a, b, e) {
        return U.current.useSyncExternalStore(a, b, e);
      };
      exports.useTransition = function() {
        return U.current.useTransition();
      };
      exports.version = "18.3.1";
    }
  });

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react/index.js
  var require_react = __commonJS({
    "../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react/index.js"(exports, module) {
      "use strict";
      if (true) {
        module.exports = require_react_production_min();
      } else {
        module.exports = null;
      }
    }
  });

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/scheduler/cjs/scheduler.production.min.js
  var require_scheduler_production_min = __commonJS({
    "../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/scheduler/cjs/scheduler.production.min.js"(exports) {
      "use strict";
      function f(a, b) {
        var c = a.length;
        a.push(b);
        a:
          for (; 0 < c; ) {
            var d = c - 1 >>> 1, e = a[d];
            if (0 < g(e, b))
              a[d] = b, a[c] = e, c = d;
            else
              break a;
          }
      }
      function h(a) {
        return 0 === a.length ? null : a[0];
      }
      function k(a) {
        if (0 === a.length)
          return null;
        var b = a[0], c = a.pop();
        if (c !== b) {
          a[0] = c;
          a:
            for (var d = 0, e = a.length, w = e >>> 1; d < w; ) {
              var m = 2 * (d + 1) - 1, C3 = a[m], n = m + 1, x = a[n];
              if (0 > g(C3, c))
                n < e && 0 > g(x, C3) ? (a[d] = x, a[n] = c, d = n) : (a[d] = C3, a[m] = c, d = m);
              else if (n < e && 0 > g(x, c))
                a[d] = x, a[n] = c, d = n;
              else
                break a;
            }
        }
        return b;
      }
      function g(a, b) {
        var c = a.sortIndex - b.sortIndex;
        return 0 !== c ? c : a.id - b.id;
      }
      if ("object" === typeof performance && "function" === typeof performance.now) {
        l = performance;
        exports.unstable_now = function() {
          return l.now();
        };
      } else {
        p = Date, q = p.now();
        exports.unstable_now = function() {
          return p.now() - q;
        };
      }
      var l;
      var p;
      var q;
      var r = [];
      var t = [];
      var u = 1;
      var v = null;
      var y = 3;
      var z = false;
      var A = false;
      var B = false;
      var D = "function" === typeof setTimeout ? setTimeout : null;
      var E = "function" === typeof clearTimeout ? clearTimeout : null;
      var F = "undefined" !== typeof setImmediate ? setImmediate : null;
      "undefined" !== typeof navigator && void 0 !== navigator.scheduling && void 0 !== navigator.scheduling.isInputPending && navigator.scheduling.isInputPending.bind(navigator.scheduling);
      function G4(a) {
        for (var b = h(t); null !== b; ) {
          if (null === b.callback)
            k(t);
          else if (b.startTime <= a)
            k(t), b.sortIndex = b.expirationTime, f(r, b);
          else
            break;
          b = h(t);
        }
      }
      function H12(a) {
        B = false;
        G4(a);
        if (!A)
          if (null !== h(r))
            A = true, I(J);
          else {
            var b = h(t);
            null !== b && K(H12, b.startTime - a);
          }
      }
      function J(a, b) {
        A = false;
        B && (B = false, E(L), L = -1);
        z = true;
        var c = y;
        try {
          G4(b);
          for (v = h(r); null !== v && (!(v.expirationTime > b) || a && !M()); ) {
            var d = v.callback;
            if ("function" === typeof d) {
              v.callback = null;
              y = v.priorityLevel;
              var e = d(v.expirationTime <= b);
              b = exports.unstable_now();
              "function" === typeof e ? v.callback = e : v === h(r) && k(r);
              G4(b);
            } else
              k(r);
            v = h(r);
          }
          if (null !== v)
            var w = true;
          else {
            var m = h(t);
            null !== m && K(H12, m.startTime - b);
            w = false;
          }
          return w;
        } finally {
          v = null, y = c, z = false;
        }
      }
      var N = false;
      var O2 = null;
      var L = -1;
      var P = 5;
      var Q = -1;
      function M() {
        return exports.unstable_now() - Q < P ? false : true;
      }
      function R() {
        if (null !== O2) {
          var a = exports.unstable_now();
          Q = a;
          var b = true;
          try {
            b = O2(true, a);
          } finally {
            b ? S2() : (N = false, O2 = null);
          }
        } else
          N = false;
      }
      var S2;
      if ("function" === typeof F)
        S2 = function() {
          F(R);
        };
      else if ("undefined" !== typeof MessageChannel) {
        T2 = new MessageChannel(), U = T2.port2;
        T2.port1.onmessage = R;
        S2 = function() {
          U.postMessage(null);
        };
      } else
        S2 = function() {
          D(R, 0);
        };
      var T2;
      var U;
      function I(a) {
        O2 = a;
        N || (N = true, S2());
      }
      function K(a, b) {
        L = D(function() {
          a(exports.unstable_now());
        }, b);
      }
      exports.unstable_IdlePriority = 5;
      exports.unstable_ImmediatePriority = 1;
      exports.unstable_LowPriority = 4;
      exports.unstable_NormalPriority = 3;
      exports.unstable_Profiling = null;
      exports.unstable_UserBlockingPriority = 2;
      exports.unstable_cancelCallback = function(a) {
        a.callback = null;
      };
      exports.unstable_continueExecution = function() {
        A || z || (A = true, I(J));
      };
      exports.unstable_forceFrameRate = function(a) {
        0 > a || 125 < a ? console.error("forceFrameRate takes a positive int between 0 and 125, forcing frame rates higher than 125 fps is not supported") : P = 0 < a ? Math.floor(1e3 / a) : 5;
      };
      exports.unstable_getCurrentPriorityLevel = function() {
        return y;
      };
      exports.unstable_getFirstCallbackNode = function() {
        return h(r);
      };
      exports.unstable_next = function(a) {
        switch (y) {
          case 1:
          case 2:
          case 3:
            var b = 3;
            break;
          default:
            b = y;
        }
        var c = y;
        y = b;
        try {
          return a();
        } finally {
          y = c;
        }
      };
      exports.unstable_pauseExecution = function() {
      };
      exports.unstable_requestPaint = function() {
      };
      exports.unstable_runWithPriority = function(a, b) {
        switch (a) {
          case 1:
          case 2:
          case 3:
          case 4:
          case 5:
            break;
          default:
            a = 3;
        }
        var c = y;
        y = a;
        try {
          return b();
        } finally {
          y = c;
        }
      };
      exports.unstable_scheduleCallback = function(a, b, c) {
        var d = exports.unstable_now();
        "object" === typeof c && null !== c ? (c = c.delay, c = "number" === typeof c && 0 < c ? d + c : d) : c = d;
        switch (a) {
          case 1:
            var e = -1;
            break;
          case 2:
            e = 250;
            break;
          case 5:
            e = 1073741823;
            break;
          case 4:
            e = 1e4;
            break;
          default:
            e = 5e3;
        }
        e = c + e;
        a = { id: u++, callback: b, priorityLevel: a, startTime: c, expirationTime: e, sortIndex: -1 };
        c > d ? (a.sortIndex = c, f(t, a), null === h(r) && a === h(t) && (B ? (E(L), L = -1) : B = true, K(H12, c - d))) : (a.sortIndex = e, f(r, a), A || z || (A = true, I(J)));
        return a;
      };
      exports.unstable_shouldYield = M;
      exports.unstable_wrapCallback = function(a) {
        var b = y;
        return function() {
          var c = y;
          y = b;
          try {
            return a.apply(this, arguments);
          } finally {
            y = c;
          }
        };
      };
    }
  });

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/scheduler/index.js
  var require_scheduler = __commonJS({
    "../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/scheduler/index.js"(exports, module) {
      "use strict";
      if (true) {
        module.exports = require_scheduler_production_min();
      } else {
        module.exports = null;
      }
    }
  });

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react-dom/cjs/react-dom.production.min.js
  var require_react_dom_production_min = __commonJS({
    "../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react-dom/cjs/react-dom.production.min.js"(exports) {
      "use strict";
      var aa = require_react();
      var ca = require_scheduler();
      function p(a) {
        for (var b = "https://reactjs.org/docs/error-decoder.html?invariant=" + a, c = 1; c < arguments.length; c++)
          b += "&args[]=" + encodeURIComponent(arguments[c]);
        return "Minified React error #" + a + "; visit " + b + " for the full message or use the non-minified dev environment for full errors and additional helpful warnings.";
      }
      var da = /* @__PURE__ */ new Set();
      var ea = {};
      function fa(a, b) {
        ha(a, b);
        ha(a + "Capture", b);
      }
      function ha(a, b) {
        ea[a] = b;
        for (a = 0; a < b.length; a++)
          da.add(b[a]);
      }
      var ia = !("undefined" === typeof window || "undefined" === typeof window.document || "undefined" === typeof window.document.createElement);
      var ja = Object.prototype.hasOwnProperty;
      var ka = /^[:A-Z_a-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u02FF\u0370-\u037D\u037F-\u1FFF\u200C-\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD][:A-Z_a-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u02FF\u0370-\u037D\u037F-\u1FFF\u200C-\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD\-.0-9\u00B7\u0300-\u036F\u203F-\u2040]*$/;
      var la = {};
      var ma = {};
      function oa(a) {
        if (ja.call(ma, a))
          return true;
        if (ja.call(la, a))
          return false;
        if (ka.test(a))
          return ma[a] = true;
        la[a] = true;
        return false;
      }
      function pa(a, b, c, d) {
        if (null !== c && 0 === c.type)
          return false;
        switch (typeof b) {
          case "function":
          case "symbol":
            return true;
          case "boolean":
            if (d)
              return false;
            if (null !== c)
              return !c.acceptsBooleans;
            a = a.toLowerCase().slice(0, 5);
            return "data-" !== a && "aria-" !== a;
          default:
            return false;
        }
      }
      function qa(a, b, c, d) {
        if (null === b || "undefined" === typeof b || pa(a, b, c, d))
          return true;
        if (d)
          return false;
        if (null !== c)
          switch (c.type) {
            case 3:
              return !b;
            case 4:
              return false === b;
            case 5:
              return isNaN(b);
            case 6:
              return isNaN(b) || 1 > b;
          }
        return false;
      }
      function v(a, b, c, d, e, f, g) {
        this.acceptsBooleans = 2 === b || 3 === b || 4 === b;
        this.attributeName = d;
        this.attributeNamespace = e;
        this.mustUseProperty = c;
        this.propertyName = a;
        this.type = b;
        this.sanitizeURL = f;
        this.removeEmptyString = g;
      }
      var z = {};
      "children dangerouslySetInnerHTML defaultValue defaultChecked innerHTML suppressContentEditableWarning suppressHydrationWarning style".split(" ").forEach(function(a) {
        z[a] = new v(a, 0, false, a, null, false, false);
      });
      [["acceptCharset", "accept-charset"], ["className", "class"], ["htmlFor", "for"], ["httpEquiv", "http-equiv"]].forEach(function(a) {
        var b = a[0];
        z[b] = new v(b, 1, false, a[1], null, false, false);
      });
      ["contentEditable", "draggable", "spellCheck", "value"].forEach(function(a) {
        z[a] = new v(a, 2, false, a.toLowerCase(), null, false, false);
      });
      ["autoReverse", "externalResourcesRequired", "focusable", "preserveAlpha"].forEach(function(a) {
        z[a] = new v(a, 2, false, a, null, false, false);
      });
      "allowFullScreen async autoFocus autoPlay controls default defer disabled disablePictureInPicture disableRemotePlayback formNoValidate hidden loop noModule noValidate open playsInline readOnly required reversed scoped seamless itemScope".split(" ").forEach(function(a) {
        z[a] = new v(a, 3, false, a.toLowerCase(), null, false, false);
      });
      ["checked", "multiple", "muted", "selected"].forEach(function(a) {
        z[a] = new v(a, 3, true, a, null, false, false);
      });
      ["capture", "download"].forEach(function(a) {
        z[a] = new v(a, 4, false, a, null, false, false);
      });
      ["cols", "rows", "size", "span"].forEach(function(a) {
        z[a] = new v(a, 6, false, a, null, false, false);
      });
      ["rowSpan", "start"].forEach(function(a) {
        z[a] = new v(a, 5, false, a.toLowerCase(), null, false, false);
      });
      var ra = /[\-:]([a-z])/g;
      function sa(a) {
        return a[1].toUpperCase();
      }
      "accent-height alignment-baseline arabic-form baseline-shift cap-height clip-path clip-rule color-interpolation color-interpolation-filters color-profile color-rendering dominant-baseline enable-background fill-opacity fill-rule flood-color flood-opacity font-family font-size font-size-adjust font-stretch font-style font-variant font-weight glyph-name glyph-orientation-horizontal glyph-orientation-vertical horiz-adv-x horiz-origin-x image-rendering letter-spacing lighting-color marker-end marker-mid marker-start overline-position overline-thickness paint-order panose-1 pointer-events rendering-intent shape-rendering stop-color stop-opacity strikethrough-position strikethrough-thickness stroke-dasharray stroke-dashoffset stroke-linecap stroke-linejoin stroke-miterlimit stroke-opacity stroke-width text-anchor text-decoration text-rendering underline-position underline-thickness unicode-bidi unicode-range units-per-em v-alphabetic v-hanging v-ideographic v-mathematical vector-effect vert-adv-y vert-origin-x vert-origin-y word-spacing writing-mode xmlns:xlink x-height".split(" ").forEach(function(a) {
        var b = a.replace(
          ra,
          sa
        );
        z[b] = new v(b, 1, false, a, null, false, false);
      });
      "xlink:actuate xlink:arcrole xlink:role xlink:show xlink:title xlink:type".split(" ").forEach(function(a) {
        var b = a.replace(ra, sa);
        z[b] = new v(b, 1, false, a, "http://www.w3.org/1999/xlink", false, false);
      });
      ["xml:base", "xml:lang", "xml:space"].forEach(function(a) {
        var b = a.replace(ra, sa);
        z[b] = new v(b, 1, false, a, "http://www.w3.org/XML/1998/namespace", false, false);
      });
      ["tabIndex", "crossOrigin"].forEach(function(a) {
        z[a] = new v(a, 1, false, a.toLowerCase(), null, false, false);
      });
      z.xlinkHref = new v("xlinkHref", 1, false, "xlink:href", "http://www.w3.org/1999/xlink", true, false);
      ["src", "href", "action", "formAction"].forEach(function(a) {
        z[a] = new v(a, 1, false, a.toLowerCase(), null, true, true);
      });
      function ta(a, b, c, d) {
        var e = z.hasOwnProperty(b) ? z[b] : null;
        if (null !== e ? 0 !== e.type : d || !(2 < b.length) || "o" !== b[0] && "O" !== b[0] || "n" !== b[1] && "N" !== b[1])
          qa(b, c, e, d) && (c = null), d || null === e ? oa(b) && (null === c ? a.removeAttribute(b) : a.setAttribute(b, "" + c)) : e.mustUseProperty ? a[e.propertyName] = null === c ? 3 === e.type ? false : "" : c : (b = e.attributeName, d = e.attributeNamespace, null === c ? a.removeAttribute(b) : (e = e.type, c = 3 === e || 4 === e && true === c ? "" : "" + c, d ? a.setAttributeNS(d, b, c) : a.setAttribute(b, c)));
      }
      var ua = aa.__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED;
      var va = Symbol.for("react.element");
      var wa = Symbol.for("react.portal");
      var ya = Symbol.for("react.fragment");
      var za = Symbol.for("react.strict_mode");
      var Aa = Symbol.for("react.profiler");
      var Ba = Symbol.for("react.provider");
      var Ca = Symbol.for("react.context");
      var Da = Symbol.for("react.forward_ref");
      var Ea = Symbol.for("react.suspense");
      var Fa = Symbol.for("react.suspense_list");
      var Ga = Symbol.for("react.memo");
      var Ha = Symbol.for("react.lazy");
      Symbol.for("react.scope");
      Symbol.for("react.debug_trace_mode");
      var Ia = Symbol.for("react.offscreen");
      Symbol.for("react.legacy_hidden");
      Symbol.for("react.cache");
      Symbol.for("react.tracing_marker");
      var Ja = Symbol.iterator;
      function Ka(a) {
        if (null === a || "object" !== typeof a)
          return null;
        a = Ja && a[Ja] || a["@@iterator"];
        return "function" === typeof a ? a : null;
      }
      var A = Object.assign;
      var La;
      function Ma(a) {
        if (void 0 === La)
          try {
            throw Error();
          } catch (c) {
            var b = c.stack.trim().match(/\n( *(at )?)/);
            La = b && b[1] || "";
          }
        return "\n" + La + a;
      }
      var Na = false;
      function Oa(a, b) {
        if (!a || Na)
          return "";
        Na = true;
        var c = Error.prepareStackTrace;
        Error.prepareStackTrace = void 0;
        try {
          if (b)
            if (b = function() {
              throw Error();
            }, Object.defineProperty(b.prototype, "props", { set: function() {
              throw Error();
            } }), "object" === typeof Reflect && Reflect.construct) {
              try {
                Reflect.construct(b, []);
              } catch (l) {
                var d = l;
              }
              Reflect.construct(a, [], b);
            } else {
              try {
                b.call();
              } catch (l) {
                d = l;
              }
              a.call(b.prototype);
            }
          else {
            try {
              throw Error();
            } catch (l) {
              d = l;
            }
            a();
          }
        } catch (l) {
          if (l && d && "string" === typeof l.stack) {
            for (var e = l.stack.split("\n"), f = d.stack.split("\n"), g = e.length - 1, h = f.length - 1; 1 <= g && 0 <= h && e[g] !== f[h]; )
              h--;
            for (; 1 <= g && 0 <= h; g--, h--)
              if (e[g] !== f[h]) {
                if (1 !== g || 1 !== h) {
                  do
                    if (g--, h--, 0 > h || e[g] !== f[h]) {
                      var k = "\n" + e[g].replace(" at new ", " at ");
                      a.displayName && k.includes("<anonymous>") && (k = k.replace("<anonymous>", a.displayName));
                      return k;
                    }
                  while (1 <= g && 0 <= h);
                }
                break;
              }
          }
        } finally {
          Na = false, Error.prepareStackTrace = c;
        }
        return (a = a ? a.displayName || a.name : "") ? Ma(a) : "";
      }
      function Pa(a) {
        switch (a.tag) {
          case 5:
            return Ma(a.type);
          case 16:
            return Ma("Lazy");
          case 13:
            return Ma("Suspense");
          case 19:
            return Ma("SuspenseList");
          case 0:
          case 2:
          case 15:
            return a = Oa(a.type, false), a;
          case 11:
            return a = Oa(a.type.render, false), a;
          case 1:
            return a = Oa(a.type, true), a;
          default:
            return "";
        }
      }
      function Qa(a) {
        if (null == a)
          return null;
        if ("function" === typeof a)
          return a.displayName || a.name || null;
        if ("string" === typeof a)
          return a;
        switch (a) {
          case ya:
            return "Fragment";
          case wa:
            return "Portal";
          case Aa:
            return "Profiler";
          case za:
            return "StrictMode";
          case Ea:
            return "Suspense";
          case Fa:
            return "SuspenseList";
        }
        if ("object" === typeof a)
          switch (a.$$typeof) {
            case Ca:
              return (a.displayName || "Context") + ".Consumer";
            case Ba:
              return (a._context.displayName || "Context") + ".Provider";
            case Da:
              var b = a.render;
              a = a.displayName;
              a || (a = b.displayName || b.name || "", a = "" !== a ? "ForwardRef(" + a + ")" : "ForwardRef");
              return a;
            case Ga:
              return b = a.displayName || null, null !== b ? b : Qa(a.type) || "Memo";
            case Ha:
              b = a._payload;
              a = a._init;
              try {
                return Qa(a(b));
              } catch (c) {
              }
          }
        return null;
      }
      function Ra(a) {
        var b = a.type;
        switch (a.tag) {
          case 24:
            return "Cache";
          case 9:
            return (b.displayName || "Context") + ".Consumer";
          case 10:
            return (b._context.displayName || "Context") + ".Provider";
          case 18:
            return "DehydratedFragment";
          case 11:
            return a = b.render, a = a.displayName || a.name || "", b.displayName || ("" !== a ? "ForwardRef(" + a + ")" : "ForwardRef");
          case 7:
            return "Fragment";
          case 5:
            return b;
          case 4:
            return "Portal";
          case 3:
            return "Root";
          case 6:
            return "Text";
          case 16:
            return Qa(b);
          case 8:
            return b === za ? "StrictMode" : "Mode";
          case 22:
            return "Offscreen";
          case 12:
            return "Profiler";
          case 21:
            return "Scope";
          case 13:
            return "Suspense";
          case 19:
            return "SuspenseList";
          case 25:
            return "TracingMarker";
          case 1:
          case 0:
          case 17:
          case 2:
          case 14:
          case 15:
            if ("function" === typeof b)
              return b.displayName || b.name || null;
            if ("string" === typeof b)
              return b;
        }
        return null;
      }
      function Sa(a) {
        switch (typeof a) {
          case "boolean":
          case "number":
          case "string":
          case "undefined":
            return a;
          case "object":
            return a;
          default:
            return "";
        }
      }
      function Ta(a) {
        var b = a.type;
        return (a = a.nodeName) && "input" === a.toLowerCase() && ("checkbox" === b || "radio" === b);
      }
      function Ua(a) {
        var b = Ta(a) ? "checked" : "value", c = Object.getOwnPropertyDescriptor(a.constructor.prototype, b), d = "" + a[b];
        if (!a.hasOwnProperty(b) && "undefined" !== typeof c && "function" === typeof c.get && "function" === typeof c.set) {
          var e = c.get, f = c.set;
          Object.defineProperty(a, b, { configurable: true, get: function() {
            return e.call(this);
          }, set: function(a2) {
            d = "" + a2;
            f.call(this, a2);
          } });
          Object.defineProperty(a, b, { enumerable: c.enumerable });
          return { getValue: function() {
            return d;
          }, setValue: function(a2) {
            d = "" + a2;
          }, stopTracking: function() {
            a._valueTracker = null;
            delete a[b];
          } };
        }
      }
      function Va(a) {
        a._valueTracker || (a._valueTracker = Ua(a));
      }
      function Wa(a) {
        if (!a)
          return false;
        var b = a._valueTracker;
        if (!b)
          return true;
        var c = b.getValue();
        var d = "";
        a && (d = Ta(a) ? a.checked ? "true" : "false" : a.value);
        a = d;
        return a !== c ? (b.setValue(a), true) : false;
      }
      function Xa(a) {
        a = a || ("undefined" !== typeof document ? document : void 0);
        if ("undefined" === typeof a)
          return null;
        try {
          return a.activeElement || a.body;
        } catch (b) {
          return a.body;
        }
      }
      function Ya(a, b) {
        var c = b.checked;
        return A({}, b, { defaultChecked: void 0, defaultValue: void 0, value: void 0, checked: null != c ? c : a._wrapperState.initialChecked });
      }
      function Za(a, b) {
        var c = null == b.defaultValue ? "" : b.defaultValue, d = null != b.checked ? b.checked : b.defaultChecked;
        c = Sa(null != b.value ? b.value : c);
        a._wrapperState = { initialChecked: d, initialValue: c, controlled: "checkbox" === b.type || "radio" === b.type ? null != b.checked : null != b.value };
      }
      function ab(a, b) {
        b = b.checked;
        null != b && ta(a, "checked", b, false);
      }
      function bb(a, b) {
        ab(a, b);
        var c = Sa(b.value), d = b.type;
        if (null != c)
          if ("number" === d) {
            if (0 === c && "" === a.value || a.value != c)
              a.value = "" + c;
          } else
            a.value !== "" + c && (a.value = "" + c);
        else if ("submit" === d || "reset" === d) {
          a.removeAttribute("value");
          return;
        }
        b.hasOwnProperty("value") ? cb(a, b.type, c) : b.hasOwnProperty("defaultValue") && cb(a, b.type, Sa(b.defaultValue));
        null == b.checked && null != b.defaultChecked && (a.defaultChecked = !!b.defaultChecked);
      }
      function db(a, b, c) {
        if (b.hasOwnProperty("value") || b.hasOwnProperty("defaultValue")) {
          var d = b.type;
          if (!("submit" !== d && "reset" !== d || void 0 !== b.value && null !== b.value))
            return;
          b = "" + a._wrapperState.initialValue;
          c || b === a.value || (a.value = b);
          a.defaultValue = b;
        }
        c = a.name;
        "" !== c && (a.name = "");
        a.defaultChecked = !!a._wrapperState.initialChecked;
        "" !== c && (a.name = c);
      }
      function cb(a, b, c) {
        if ("number" !== b || Xa(a.ownerDocument) !== a)
          null == c ? a.defaultValue = "" + a._wrapperState.initialValue : a.defaultValue !== "" + c && (a.defaultValue = "" + c);
      }
      var eb = Array.isArray;
      function fb(a, b, c, d) {
        a = a.options;
        if (b) {
          b = {};
          for (var e = 0; e < c.length; e++)
            b["$" + c[e]] = true;
          for (c = 0; c < a.length; c++)
            e = b.hasOwnProperty("$" + a[c].value), a[c].selected !== e && (a[c].selected = e), e && d && (a[c].defaultSelected = true);
        } else {
          c = "" + Sa(c);
          b = null;
          for (e = 0; e < a.length; e++) {
            if (a[e].value === c) {
              a[e].selected = true;
              d && (a[e].defaultSelected = true);
              return;
            }
            null !== b || a[e].disabled || (b = a[e]);
          }
          null !== b && (b.selected = true);
        }
      }
      function gb(a, b) {
        if (null != b.dangerouslySetInnerHTML)
          throw Error(p(91));
        return A({}, b, { value: void 0, defaultValue: void 0, children: "" + a._wrapperState.initialValue });
      }
      function hb(a, b) {
        var c = b.value;
        if (null == c) {
          c = b.children;
          b = b.defaultValue;
          if (null != c) {
            if (null != b)
              throw Error(p(92));
            if (eb(c)) {
              if (1 < c.length)
                throw Error(p(93));
              c = c[0];
            }
            b = c;
          }
          null == b && (b = "");
          c = b;
        }
        a._wrapperState = { initialValue: Sa(c) };
      }
      function ib(a, b) {
        var c = Sa(b.value), d = Sa(b.defaultValue);
        null != c && (c = "" + c, c !== a.value && (a.value = c), null == b.defaultValue && a.defaultValue !== c && (a.defaultValue = c));
        null != d && (a.defaultValue = "" + d);
      }
      function jb(a) {
        var b = a.textContent;
        b === a._wrapperState.initialValue && "" !== b && null !== b && (a.value = b);
      }
      function kb(a) {
        switch (a) {
          case "svg":
            return "http://www.w3.org/2000/svg";
          case "math":
            return "http://www.w3.org/1998/Math/MathML";
          default:
            return "http://www.w3.org/1999/xhtml";
        }
      }
      function lb(a, b) {
        return null == a || "http://www.w3.org/1999/xhtml" === a ? kb(b) : "http://www.w3.org/2000/svg" === a && "foreignObject" === b ? "http://www.w3.org/1999/xhtml" : a;
      }
      var mb;
      var nb = function(a) {
        return "undefined" !== typeof MSApp && MSApp.execUnsafeLocalFunction ? function(b, c, d, e) {
          MSApp.execUnsafeLocalFunction(function() {
            return a(b, c, d, e);
          });
        } : a;
      }(function(a, b) {
        if ("http://www.w3.org/2000/svg" !== a.namespaceURI || "innerHTML" in a)
          a.innerHTML = b;
        else {
          mb = mb || document.createElement("div");
          mb.innerHTML = "<svg>" + b.valueOf().toString() + "</svg>";
          for (b = mb.firstChild; a.firstChild; )
            a.removeChild(a.firstChild);
          for (; b.firstChild; )
            a.appendChild(b.firstChild);
        }
      });
      function ob(a, b) {
        if (b) {
          var c = a.firstChild;
          if (c && c === a.lastChild && 3 === c.nodeType) {
            c.nodeValue = b;
            return;
          }
        }
        a.textContent = b;
      }
      var pb = {
        animationIterationCount: true,
        aspectRatio: true,
        borderImageOutset: true,
        borderImageSlice: true,
        borderImageWidth: true,
        boxFlex: true,
        boxFlexGroup: true,
        boxOrdinalGroup: true,
        columnCount: true,
        columns: true,
        flex: true,
        flexGrow: true,
        flexPositive: true,
        flexShrink: true,
        flexNegative: true,
        flexOrder: true,
        gridArea: true,
        gridRow: true,
        gridRowEnd: true,
        gridRowSpan: true,
        gridRowStart: true,
        gridColumn: true,
        gridColumnEnd: true,
        gridColumnSpan: true,
        gridColumnStart: true,
        fontWeight: true,
        lineClamp: true,
        lineHeight: true,
        opacity: true,
        order: true,
        orphans: true,
        tabSize: true,
        widows: true,
        zIndex: true,
        zoom: true,
        fillOpacity: true,
        floodOpacity: true,
        stopOpacity: true,
        strokeDasharray: true,
        strokeDashoffset: true,
        strokeMiterlimit: true,
        strokeOpacity: true,
        strokeWidth: true
      };
      var qb = ["Webkit", "ms", "Moz", "O"];
      Object.keys(pb).forEach(function(a) {
        qb.forEach(function(b) {
          b = b + a.charAt(0).toUpperCase() + a.substring(1);
          pb[b] = pb[a];
        });
      });
      function rb(a, b, c) {
        return null == b || "boolean" === typeof b || "" === b ? "" : c || "number" !== typeof b || 0 === b || pb.hasOwnProperty(a) && pb[a] ? ("" + b).trim() : b + "px";
      }
      function sb(a, b) {
        a = a.style;
        for (var c in b)
          if (b.hasOwnProperty(c)) {
            var d = 0 === c.indexOf("--"), e = rb(c, b[c], d);
            "float" === c && (c = "cssFloat");
            d ? a.setProperty(c, e) : a[c] = e;
          }
      }
      var tb = A({ menuitem: true }, { area: true, base: true, br: true, col: true, embed: true, hr: true, img: true, input: true, keygen: true, link: true, meta: true, param: true, source: true, track: true, wbr: true });
      function ub(a, b) {
        if (b) {
          if (tb[a] && (null != b.children || null != b.dangerouslySetInnerHTML))
            throw Error(p(137, a));
          if (null != b.dangerouslySetInnerHTML) {
            if (null != b.children)
              throw Error(p(60));
            if ("object" !== typeof b.dangerouslySetInnerHTML || !("__html" in b.dangerouslySetInnerHTML))
              throw Error(p(61));
          }
          if (null != b.style && "object" !== typeof b.style)
            throw Error(p(62));
        }
      }
      function vb(a, b) {
        if (-1 === a.indexOf("-"))
          return "string" === typeof b.is;
        switch (a) {
          case "annotation-xml":
          case "color-profile":
          case "font-face":
          case "font-face-src":
          case "font-face-uri":
          case "font-face-format":
          case "font-face-name":
          case "missing-glyph":
            return false;
          default:
            return true;
        }
      }
      var wb = null;
      function xb(a) {
        a = a.target || a.srcElement || window;
        a.correspondingUseElement && (a = a.correspondingUseElement);
        return 3 === a.nodeType ? a.parentNode : a;
      }
      var yb = null;
      var zb = null;
      var Ab = null;
      function Bb(a) {
        if (a = Cb(a)) {
          if ("function" !== typeof yb)
            throw Error(p(280));
          var b = a.stateNode;
          b && (b = Db(b), yb(a.stateNode, a.type, b));
        }
      }
      function Eb(a) {
        zb ? Ab ? Ab.push(a) : Ab = [a] : zb = a;
      }
      function Fb() {
        if (zb) {
          var a = zb, b = Ab;
          Ab = zb = null;
          Bb(a);
          if (b)
            for (a = 0; a < b.length; a++)
              Bb(b[a]);
        }
      }
      function Gb(a, b) {
        return a(b);
      }
      function Hb() {
      }
      var Ib = false;
      function Jb(a, b, c) {
        if (Ib)
          return a(b, c);
        Ib = true;
        try {
          return Gb(a, b, c);
        } finally {
          if (Ib = false, null !== zb || null !== Ab)
            Hb(), Fb();
        }
      }
      function Kb(a, b) {
        var c = a.stateNode;
        if (null === c)
          return null;
        var d = Db(c);
        if (null === d)
          return null;
        c = d[b];
        a:
          switch (b) {
            case "onClick":
            case "onClickCapture":
            case "onDoubleClick":
            case "onDoubleClickCapture":
            case "onMouseDown":
            case "onMouseDownCapture":
            case "onMouseMove":
            case "onMouseMoveCapture":
            case "onMouseUp":
            case "onMouseUpCapture":
            case "onMouseEnter":
              (d = !d.disabled) || (a = a.type, d = !("button" === a || "input" === a || "select" === a || "textarea" === a));
              a = !d;
              break a;
            default:
              a = false;
          }
        if (a)
          return null;
        if (c && "function" !== typeof c)
          throw Error(p(231, b, typeof c));
        return c;
      }
      var Lb = false;
      if (ia)
        try {
          Mb = {};
          Object.defineProperty(Mb, "passive", { get: function() {
            Lb = true;
          } });
          window.addEventListener("test", Mb, Mb);
          window.removeEventListener("test", Mb, Mb);
        } catch (a) {
          Lb = false;
        }
      var Mb;
      function Nb(a, b, c, d, e, f, g, h, k) {
        var l = Array.prototype.slice.call(arguments, 3);
        try {
          b.apply(c, l);
        } catch (m) {
          this.onError(m);
        }
      }
      var Ob = false;
      var Pb = null;
      var Qb = false;
      var Rb = null;
      var Sb = { onError: function(a) {
        Ob = true;
        Pb = a;
      } };
      function Tb2(a, b, c, d, e, f, g, h, k) {
        Ob = false;
        Pb = null;
        Nb.apply(Sb, arguments);
      }
      function Ub(a, b, c, d, e, f, g, h, k) {
        Tb2.apply(this, arguments);
        if (Ob) {
          if (Ob) {
            var l = Pb;
            Ob = false;
            Pb = null;
          } else
            throw Error(p(198));
          Qb || (Qb = true, Rb = l);
        }
      }
      function Vb(a) {
        var b = a, c = a;
        if (a.alternate)
          for (; b.return; )
            b = b.return;
        else {
          a = b;
          do
            b = a, 0 !== (b.flags & 4098) && (c = b.return), a = b.return;
          while (a);
        }
        return 3 === b.tag ? c : null;
      }
      function Wb(a) {
        if (13 === a.tag) {
          var b = a.memoizedState;
          null === b && (a = a.alternate, null !== a && (b = a.memoizedState));
          if (null !== b)
            return b.dehydrated;
        }
        return null;
      }
      function Xb(a) {
        if (Vb(a) !== a)
          throw Error(p(188));
      }
      function Yb(a) {
        var b = a.alternate;
        if (!b) {
          b = Vb(a);
          if (null === b)
            throw Error(p(188));
          return b !== a ? null : a;
        }
        for (var c = a, d = b; ; ) {
          var e = c.return;
          if (null === e)
            break;
          var f = e.alternate;
          if (null === f) {
            d = e.return;
            if (null !== d) {
              c = d;
              continue;
            }
            break;
          }
          if (e.child === f.child) {
            for (f = e.child; f; ) {
              if (f === c)
                return Xb(e), a;
              if (f === d)
                return Xb(e), b;
              f = f.sibling;
            }
            throw Error(p(188));
          }
          if (c.return !== d.return)
            c = e, d = f;
          else {
            for (var g = false, h = e.child; h; ) {
              if (h === c) {
                g = true;
                c = e;
                d = f;
                break;
              }
              if (h === d) {
                g = true;
                d = e;
                c = f;
                break;
              }
              h = h.sibling;
            }
            if (!g) {
              for (h = f.child; h; ) {
                if (h === c) {
                  g = true;
                  c = f;
                  d = e;
                  break;
                }
                if (h === d) {
                  g = true;
                  d = f;
                  c = e;
                  break;
                }
                h = h.sibling;
              }
              if (!g)
                throw Error(p(189));
            }
          }
          if (c.alternate !== d)
            throw Error(p(190));
        }
        if (3 !== c.tag)
          throw Error(p(188));
        return c.stateNode.current === c ? a : b;
      }
      function Zb(a) {
        a = Yb(a);
        return null !== a ? $b(a) : null;
      }
      function $b(a) {
        if (5 === a.tag || 6 === a.tag)
          return a;
        for (a = a.child; null !== a; ) {
          var b = $b(a);
          if (null !== b)
            return b;
          a = a.sibling;
        }
        return null;
      }
      var ac = ca.unstable_scheduleCallback;
      var bc = ca.unstable_cancelCallback;
      var cc = ca.unstable_shouldYield;
      var dc = ca.unstable_requestPaint;
      var B = ca.unstable_now;
      var ec = ca.unstable_getCurrentPriorityLevel;
      var fc = ca.unstable_ImmediatePriority;
      var gc = ca.unstable_UserBlockingPriority;
      var hc = ca.unstable_NormalPriority;
      var ic = ca.unstable_LowPriority;
      var jc = ca.unstable_IdlePriority;
      var kc = null;
      var lc = null;
      function mc(a) {
        if (lc && "function" === typeof lc.onCommitFiberRoot)
          try {
            lc.onCommitFiberRoot(kc, a, void 0, 128 === (a.current.flags & 128));
          } catch (b) {
          }
      }
      var oc = Math.clz32 ? Math.clz32 : nc;
      var pc = Math.log;
      var qc = Math.LN2;
      function nc(a) {
        a >>>= 0;
        return 0 === a ? 32 : 31 - (pc(a) / qc | 0) | 0;
      }
      var rc = 64;
      var sc = 4194304;
      function tc(a) {
        switch (a & -a) {
          case 1:
            return 1;
          case 2:
            return 2;
          case 4:
            return 4;
          case 8:
            return 8;
          case 16:
            return 16;
          case 32:
            return 32;
          case 64:
          case 128:
          case 256:
          case 512:
          case 1024:
          case 2048:
          case 4096:
          case 8192:
          case 16384:
          case 32768:
          case 65536:
          case 131072:
          case 262144:
          case 524288:
          case 1048576:
          case 2097152:
            return a & 4194240;
          case 4194304:
          case 8388608:
          case 16777216:
          case 33554432:
          case 67108864:
            return a & 130023424;
          case 134217728:
            return 134217728;
          case 268435456:
            return 268435456;
          case 536870912:
            return 536870912;
          case 1073741824:
            return 1073741824;
          default:
            return a;
        }
      }
      function uc(a, b) {
        var c = a.pendingLanes;
        if (0 === c)
          return 0;
        var d = 0, e = a.suspendedLanes, f = a.pingedLanes, g = c & 268435455;
        if (0 !== g) {
          var h = g & ~e;
          0 !== h ? d = tc(h) : (f &= g, 0 !== f && (d = tc(f)));
        } else
          g = c & ~e, 0 !== g ? d = tc(g) : 0 !== f && (d = tc(f));
        if (0 === d)
          return 0;
        if (0 !== b && b !== d && 0 === (b & e) && (e = d & -d, f = b & -b, e >= f || 16 === e && 0 !== (f & 4194240)))
          return b;
        0 !== (d & 4) && (d |= c & 16);
        b = a.entangledLanes;
        if (0 !== b)
          for (a = a.entanglements, b &= d; 0 < b; )
            c = 31 - oc(b), e = 1 << c, d |= a[c], b &= ~e;
        return d;
      }
      function vc(a, b) {
        switch (a) {
          case 1:
          case 2:
          case 4:
            return b + 250;
          case 8:
          case 16:
          case 32:
          case 64:
          case 128:
          case 256:
          case 512:
          case 1024:
          case 2048:
          case 4096:
          case 8192:
          case 16384:
          case 32768:
          case 65536:
          case 131072:
          case 262144:
          case 524288:
          case 1048576:
          case 2097152:
            return b + 5e3;
          case 4194304:
          case 8388608:
          case 16777216:
          case 33554432:
          case 67108864:
            return -1;
          case 134217728:
          case 268435456:
          case 536870912:
          case 1073741824:
            return -1;
          default:
            return -1;
        }
      }
      function wc(a, b) {
        for (var c = a.suspendedLanes, d = a.pingedLanes, e = a.expirationTimes, f = a.pendingLanes; 0 < f; ) {
          var g = 31 - oc(f), h = 1 << g, k = e[g];
          if (-1 === k) {
            if (0 === (h & c) || 0 !== (h & d))
              e[g] = vc(h, b);
          } else
            k <= b && (a.expiredLanes |= h);
          f &= ~h;
        }
      }
      function xc(a) {
        a = a.pendingLanes & -1073741825;
        return 0 !== a ? a : a & 1073741824 ? 1073741824 : 0;
      }
      function yc() {
        var a = rc;
        rc <<= 1;
        0 === (rc & 4194240) && (rc = 64);
        return a;
      }
      function zc(a) {
        for (var b = [], c = 0; 31 > c; c++)
          b.push(a);
        return b;
      }
      function Ac(a, b, c) {
        a.pendingLanes |= b;
        536870912 !== b && (a.suspendedLanes = 0, a.pingedLanes = 0);
        a = a.eventTimes;
        b = 31 - oc(b);
        a[b] = c;
      }
      function Bc(a, b) {
        var c = a.pendingLanes & ~b;
        a.pendingLanes = b;
        a.suspendedLanes = 0;
        a.pingedLanes = 0;
        a.expiredLanes &= b;
        a.mutableReadLanes &= b;
        a.entangledLanes &= b;
        b = a.entanglements;
        var d = a.eventTimes;
        for (a = a.expirationTimes; 0 < c; ) {
          var e = 31 - oc(c), f = 1 << e;
          b[e] = 0;
          d[e] = -1;
          a[e] = -1;
          c &= ~f;
        }
      }
      function Cc(a, b) {
        var c = a.entangledLanes |= b;
        for (a = a.entanglements; c; ) {
          var d = 31 - oc(c), e = 1 << d;
          e & b | a[d] & b && (a[d] |= b);
          c &= ~e;
        }
      }
      var C3 = 0;
      function Dc(a) {
        a &= -a;
        return 1 < a ? 4 < a ? 0 !== (a & 268435455) ? 16 : 536870912 : 4 : 1;
      }
      var Ec;
      var Fc;
      var Gc;
      var Hc;
      var Ic;
      var Jc = false;
      var Kc = [];
      var Lc = null;
      var Mc = null;
      var Nc = null;
      var Oc = /* @__PURE__ */ new Map();
      var Pc = /* @__PURE__ */ new Map();
      var Qc = [];
      var Rc = "mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput copy cut paste click change contextmenu reset submit".split(" ");
      function Sc(a, b) {
        switch (a) {
          case "focusin":
          case "focusout":
            Lc = null;
            break;
          case "dragenter":
          case "dragleave":
            Mc = null;
            break;
          case "mouseover":
          case "mouseout":
            Nc = null;
            break;
          case "pointerover":
          case "pointerout":
            Oc.delete(b.pointerId);
            break;
          case "gotpointercapture":
          case "lostpointercapture":
            Pc.delete(b.pointerId);
        }
      }
      function Tc(a, b, c, d, e, f) {
        if (null === a || a.nativeEvent !== f)
          return a = { blockedOn: b, domEventName: c, eventSystemFlags: d, nativeEvent: f, targetContainers: [e] }, null !== b && (b = Cb(b), null !== b && Fc(b)), a;
        a.eventSystemFlags |= d;
        b = a.targetContainers;
        null !== e && -1 === b.indexOf(e) && b.push(e);
        return a;
      }
      function Uc(a, b, c, d, e) {
        switch (b) {
          case "focusin":
            return Lc = Tc(Lc, a, b, c, d, e), true;
          case "dragenter":
            return Mc = Tc(Mc, a, b, c, d, e), true;
          case "mouseover":
            return Nc = Tc(Nc, a, b, c, d, e), true;
          case "pointerover":
            var f = e.pointerId;
            Oc.set(f, Tc(Oc.get(f) || null, a, b, c, d, e));
            return true;
          case "gotpointercapture":
            return f = e.pointerId, Pc.set(f, Tc(Pc.get(f) || null, a, b, c, d, e)), true;
        }
        return false;
      }
      function Vc(a) {
        var b = Wc(a.target);
        if (null !== b) {
          var c = Vb(b);
          if (null !== c) {
            if (b = c.tag, 13 === b) {
              if (b = Wb(c), null !== b) {
                a.blockedOn = b;
                Ic(a.priority, function() {
                  Gc(c);
                });
                return;
              }
            } else if (3 === b && c.stateNode.current.memoizedState.isDehydrated) {
              a.blockedOn = 3 === c.tag ? c.stateNode.containerInfo : null;
              return;
            }
          }
        }
        a.blockedOn = null;
      }
      function Xc(a) {
        if (null !== a.blockedOn)
          return false;
        for (var b = a.targetContainers; 0 < b.length; ) {
          var c = Yc(a.domEventName, a.eventSystemFlags, b[0], a.nativeEvent);
          if (null === c) {
            c = a.nativeEvent;
            var d = new c.constructor(c.type, c);
            wb = d;
            c.target.dispatchEvent(d);
            wb = null;
          } else
            return b = Cb(c), null !== b && Fc(b), a.blockedOn = c, false;
          b.shift();
        }
        return true;
      }
      function Zc(a, b, c) {
        Xc(a) && c.delete(b);
      }
      function $c() {
        Jc = false;
        null !== Lc && Xc(Lc) && (Lc = null);
        null !== Mc && Xc(Mc) && (Mc = null);
        null !== Nc && Xc(Nc) && (Nc = null);
        Oc.forEach(Zc);
        Pc.forEach(Zc);
      }
      function ad(a, b) {
        a.blockedOn === b && (a.blockedOn = null, Jc || (Jc = true, ca.unstable_scheduleCallback(ca.unstable_NormalPriority, $c)));
      }
      function bd(a) {
        function b(b2) {
          return ad(b2, a);
        }
        if (0 < Kc.length) {
          ad(Kc[0], a);
          for (var c = 1; c < Kc.length; c++) {
            var d = Kc[c];
            d.blockedOn === a && (d.blockedOn = null);
          }
        }
        null !== Lc && ad(Lc, a);
        null !== Mc && ad(Mc, a);
        null !== Nc && ad(Nc, a);
        Oc.forEach(b);
        Pc.forEach(b);
        for (c = 0; c < Qc.length; c++)
          d = Qc[c], d.blockedOn === a && (d.blockedOn = null);
        for (; 0 < Qc.length && (c = Qc[0], null === c.blockedOn); )
          Vc(c), null === c.blockedOn && Qc.shift();
      }
      var cd = ua.ReactCurrentBatchConfig;
      var dd = true;
      function ed(a, b, c, d) {
        var e = C3, f = cd.transition;
        cd.transition = null;
        try {
          C3 = 1, fd(a, b, c, d);
        } finally {
          C3 = e, cd.transition = f;
        }
      }
      function gd(a, b, c, d) {
        var e = C3, f = cd.transition;
        cd.transition = null;
        try {
          C3 = 4, fd(a, b, c, d);
        } finally {
          C3 = e, cd.transition = f;
        }
      }
      function fd(a, b, c, d) {
        if (dd) {
          var e = Yc(a, b, c, d);
          if (null === e)
            hd(a, b, d, id, c), Sc(a, d);
          else if (Uc(e, a, b, c, d))
            d.stopPropagation();
          else if (Sc(a, d), b & 4 && -1 < Rc.indexOf(a)) {
            for (; null !== e; ) {
              var f = Cb(e);
              null !== f && Ec(f);
              f = Yc(a, b, c, d);
              null === f && hd(a, b, d, id, c);
              if (f === e)
                break;
              e = f;
            }
            null !== e && d.stopPropagation();
          } else
            hd(a, b, d, null, c);
        }
      }
      var id = null;
      function Yc(a, b, c, d) {
        id = null;
        a = xb(d);
        a = Wc(a);
        if (null !== a)
          if (b = Vb(a), null === b)
            a = null;
          else if (c = b.tag, 13 === c) {
            a = Wb(b);
            if (null !== a)
              return a;
            a = null;
          } else if (3 === c) {
            if (b.stateNode.current.memoizedState.isDehydrated)
              return 3 === b.tag ? b.stateNode.containerInfo : null;
            a = null;
          } else
            b !== a && (a = null);
        id = a;
        return null;
      }
      function jd(a) {
        switch (a) {
          case "cancel":
          case "click":
          case "close":
          case "contextmenu":
          case "copy":
          case "cut":
          case "auxclick":
          case "dblclick":
          case "dragend":
          case "dragstart":
          case "drop":
          case "focusin":
          case "focusout":
          case "input":
          case "invalid":
          case "keydown":
          case "keypress":
          case "keyup":
          case "mousedown":
          case "mouseup":
          case "paste":
          case "pause":
          case "play":
          case "pointercancel":
          case "pointerdown":
          case "pointerup":
          case "ratechange":
          case "reset":
          case "resize":
          case "seeked":
          case "submit":
          case "touchcancel":
          case "touchend":
          case "touchstart":
          case "volumechange":
          case "change":
          case "selectionchange":
          case "textInput":
          case "compositionstart":
          case "compositionend":
          case "compositionupdate":
          case "beforeblur":
          case "afterblur":
          case "beforeinput":
          case "blur":
          case "fullscreenchange":
          case "focus":
          case "hashchange":
          case "popstate":
          case "select":
          case "selectstart":
            return 1;
          case "drag":
          case "dragenter":
          case "dragexit":
          case "dragleave":
          case "dragover":
          case "mousemove":
          case "mouseout":
          case "mouseover":
          case "pointermove":
          case "pointerout":
          case "pointerover":
          case "scroll":
          case "toggle":
          case "touchmove":
          case "wheel":
          case "mouseenter":
          case "mouseleave":
          case "pointerenter":
          case "pointerleave":
            return 4;
          case "message":
            switch (ec()) {
              case fc:
                return 1;
              case gc:
                return 4;
              case hc:
              case ic:
                return 16;
              case jc:
                return 536870912;
              default:
                return 16;
            }
          default:
            return 16;
        }
      }
      var kd = null;
      var ld = null;
      var md = null;
      function nd() {
        if (md)
          return md;
        var a, b = ld, c = b.length, d, e = "value" in kd ? kd.value : kd.textContent, f = e.length;
        for (a = 0; a < c && b[a] === e[a]; a++)
          ;
        var g = c - a;
        for (d = 1; d <= g && b[c - d] === e[f - d]; d++)
          ;
        return md = e.slice(a, 1 < d ? 1 - d : void 0);
      }
      function od(a) {
        var b = a.keyCode;
        "charCode" in a ? (a = a.charCode, 0 === a && 13 === b && (a = 13)) : a = b;
        10 === a && (a = 13);
        return 32 <= a || 13 === a ? a : 0;
      }
      function pd() {
        return true;
      }
      function qd() {
        return false;
      }
      function rd(a) {
        function b(b2, d, e, f, g) {
          this._reactName = b2;
          this._targetInst = e;
          this.type = d;
          this.nativeEvent = f;
          this.target = g;
          this.currentTarget = null;
          for (var c in a)
            a.hasOwnProperty(c) && (b2 = a[c], this[c] = b2 ? b2(f) : f[c]);
          this.isDefaultPrevented = (null != f.defaultPrevented ? f.defaultPrevented : false === f.returnValue) ? pd : qd;
          this.isPropagationStopped = qd;
          return this;
        }
        A(b.prototype, { preventDefault: function() {
          this.defaultPrevented = true;
          var a2 = this.nativeEvent;
          a2 && (a2.preventDefault ? a2.preventDefault() : "unknown" !== typeof a2.returnValue && (a2.returnValue = false), this.isDefaultPrevented = pd);
        }, stopPropagation: function() {
          var a2 = this.nativeEvent;
          a2 && (a2.stopPropagation ? a2.stopPropagation() : "unknown" !== typeof a2.cancelBubble && (a2.cancelBubble = true), this.isPropagationStopped = pd);
        }, persist: function() {
        }, isPersistent: pd });
        return b;
      }
      var sd = { eventPhase: 0, bubbles: 0, cancelable: 0, timeStamp: function(a) {
        return a.timeStamp || Date.now();
      }, defaultPrevented: 0, isTrusted: 0 };
      var td = rd(sd);
      var ud = A({}, sd, { view: 0, detail: 0 });
      var vd = rd(ud);
      var wd;
      var xd;
      var yd;
      var Ad = A({}, ud, { screenX: 0, screenY: 0, clientX: 0, clientY: 0, pageX: 0, pageY: 0, ctrlKey: 0, shiftKey: 0, altKey: 0, metaKey: 0, getModifierState: zd, button: 0, buttons: 0, relatedTarget: function(a) {
        return void 0 === a.relatedTarget ? a.fromElement === a.srcElement ? a.toElement : a.fromElement : a.relatedTarget;
      }, movementX: function(a) {
        if ("movementX" in a)
          return a.movementX;
        a !== yd && (yd && "mousemove" === a.type ? (wd = a.screenX - yd.screenX, xd = a.screenY - yd.screenY) : xd = wd = 0, yd = a);
        return wd;
      }, movementY: function(a) {
        return "movementY" in a ? a.movementY : xd;
      } });
      var Bd = rd(Ad);
      var Cd = A({}, Ad, { dataTransfer: 0 });
      var Dd = rd(Cd);
      var Ed = A({}, ud, { relatedTarget: 0 });
      var Fd = rd(Ed);
      var Gd = A({}, sd, { animationName: 0, elapsedTime: 0, pseudoElement: 0 });
      var Hd = rd(Gd);
      var Id = A({}, sd, { clipboardData: function(a) {
        return "clipboardData" in a ? a.clipboardData : window.clipboardData;
      } });
      var Jd = rd(Id);
      var Kd = A({}, sd, { data: 0 });
      var Ld = rd(Kd);
      var Md = {
        Esc: "Escape",
        Spacebar: " ",
        Left: "ArrowLeft",
        Up: "ArrowUp",
        Right: "ArrowRight",
        Down: "ArrowDown",
        Del: "Delete",
        Win: "OS",
        Menu: "ContextMenu",
        Apps: "ContextMenu",
        Scroll: "ScrollLock",
        MozPrintableKey: "Unidentified"
      };
      var Nd = {
        8: "Backspace",
        9: "Tab",
        12: "Clear",
        13: "Enter",
        16: "Shift",
        17: "Control",
        18: "Alt",
        19: "Pause",
        20: "CapsLock",
        27: "Escape",
        32: " ",
        33: "PageUp",
        34: "PageDown",
        35: "End",
        36: "Home",
        37: "ArrowLeft",
        38: "ArrowUp",
        39: "ArrowRight",
        40: "ArrowDown",
        45: "Insert",
        46: "Delete",
        112: "F1",
        113: "F2",
        114: "F3",
        115: "F4",
        116: "F5",
        117: "F6",
        118: "F7",
        119: "F8",
        120: "F9",
        121: "F10",
        122: "F11",
        123: "F12",
        144: "NumLock",
        145: "ScrollLock",
        224: "Meta"
      };
      var Od = { Alt: "altKey", Control: "ctrlKey", Meta: "metaKey", Shift: "shiftKey" };
      function Pd(a) {
        var b = this.nativeEvent;
        return b.getModifierState ? b.getModifierState(a) : (a = Od[a]) ? !!b[a] : false;
      }
      function zd() {
        return Pd;
      }
      var Qd = A({}, ud, { key: function(a) {
        if (a.key) {
          var b = Md[a.key] || a.key;
          if ("Unidentified" !== b)
            return b;
        }
        return "keypress" === a.type ? (a = od(a), 13 === a ? "Enter" : String.fromCharCode(a)) : "keydown" === a.type || "keyup" === a.type ? Nd[a.keyCode] || "Unidentified" : "";
      }, code: 0, location: 0, ctrlKey: 0, shiftKey: 0, altKey: 0, metaKey: 0, repeat: 0, locale: 0, getModifierState: zd, charCode: function(a) {
        return "keypress" === a.type ? od(a) : 0;
      }, keyCode: function(a) {
        return "keydown" === a.type || "keyup" === a.type ? a.keyCode : 0;
      }, which: function(a) {
        return "keypress" === a.type ? od(a) : "keydown" === a.type || "keyup" === a.type ? a.keyCode : 0;
      } });
      var Rd = rd(Qd);
      var Sd = A({}, Ad, { pointerId: 0, width: 0, height: 0, pressure: 0, tangentialPressure: 0, tiltX: 0, tiltY: 0, twist: 0, pointerType: 0, isPrimary: 0 });
      var Td = rd(Sd);
      var Ud = A({}, ud, { touches: 0, targetTouches: 0, changedTouches: 0, altKey: 0, metaKey: 0, ctrlKey: 0, shiftKey: 0, getModifierState: zd });
      var Vd = rd(Ud);
      var Wd = A({}, sd, { propertyName: 0, elapsedTime: 0, pseudoElement: 0 });
      var Xd = rd(Wd);
      var Yd = A({}, Ad, {
        deltaX: function(a) {
          return "deltaX" in a ? a.deltaX : "wheelDeltaX" in a ? -a.wheelDeltaX : 0;
        },
        deltaY: function(a) {
          return "deltaY" in a ? a.deltaY : "wheelDeltaY" in a ? -a.wheelDeltaY : "wheelDelta" in a ? -a.wheelDelta : 0;
        },
        deltaZ: 0,
        deltaMode: 0
      });
      var Zd = rd(Yd);
      var $d = [9, 13, 27, 32];
      var ae = ia && "CompositionEvent" in window;
      var be = null;
      ia && "documentMode" in document && (be = document.documentMode);
      var ce = ia && "TextEvent" in window && !be;
      var de = ia && (!ae || be && 8 < be && 11 >= be);
      var ee = String.fromCharCode(32);
      var fe = false;
      function ge(a, b) {
        switch (a) {
          case "keyup":
            return -1 !== $d.indexOf(b.keyCode);
          case "keydown":
            return 229 !== b.keyCode;
          case "keypress":
          case "mousedown":
          case "focusout":
            return true;
          default:
            return false;
        }
      }
      function he(a) {
        a = a.detail;
        return "object" === typeof a && "data" in a ? a.data : null;
      }
      var ie = false;
      function je(a, b) {
        switch (a) {
          case "compositionend":
            return he(b);
          case "keypress":
            if (32 !== b.which)
              return null;
            fe = true;
            return ee;
          case "textInput":
            return a = b.data, a === ee && fe ? null : a;
          default:
            return null;
        }
      }
      function ke(a, b) {
        if (ie)
          return "compositionend" === a || !ae && ge(a, b) ? (a = nd(), md = ld = kd = null, ie = false, a) : null;
        switch (a) {
          case "paste":
            return null;
          case "keypress":
            if (!(b.ctrlKey || b.altKey || b.metaKey) || b.ctrlKey && b.altKey) {
              if (b.char && 1 < b.char.length)
                return b.char;
              if (b.which)
                return String.fromCharCode(b.which);
            }
            return null;
          case "compositionend":
            return de && "ko" !== b.locale ? null : b.data;
          default:
            return null;
        }
      }
      var le = { color: true, date: true, datetime: true, "datetime-local": true, email: true, month: true, number: true, password: true, range: true, search: true, tel: true, text: true, time: true, url: true, week: true };
      function me(a) {
        var b = a && a.nodeName && a.nodeName.toLowerCase();
        return "input" === b ? !!le[a.type] : "textarea" === b ? true : false;
      }
      function ne(a, b, c, d) {
        Eb(d);
        b = oe(b, "onChange");
        0 < b.length && (c = new td("onChange", "change", null, c, d), a.push({ event: c, listeners: b }));
      }
      var pe = null;
      var qe = null;
      function re(a) {
        se(a, 0);
      }
      function te(a) {
        var b = ue(a);
        if (Wa(b))
          return a;
      }
      function ve(a, b) {
        if ("change" === a)
          return b;
      }
      var we = false;
      if (ia) {
        if (ia) {
          ye = "oninput" in document;
          if (!ye) {
            ze = document.createElement("div");
            ze.setAttribute("oninput", "return;");
            ye = "function" === typeof ze.oninput;
          }
          xe = ye;
        } else
          xe = false;
        we = xe && (!document.documentMode || 9 < document.documentMode);
      }
      var xe;
      var ye;
      var ze;
      function Ae() {
        pe && (pe.detachEvent("onpropertychange", Be), qe = pe = null);
      }
      function Be(a) {
        if ("value" === a.propertyName && te(qe)) {
          var b = [];
          ne(b, qe, a, xb(a));
          Jb(re, b);
        }
      }
      function Ce(a, b, c) {
        "focusin" === a ? (Ae(), pe = b, qe = c, pe.attachEvent("onpropertychange", Be)) : "focusout" === a && Ae();
      }
      function De(a) {
        if ("selectionchange" === a || "keyup" === a || "keydown" === a)
          return te(qe);
      }
      function Ee(a, b) {
        if ("click" === a)
          return te(b);
      }
      function Fe(a, b) {
        if ("input" === a || "change" === a)
          return te(b);
      }
      function Ge(a, b) {
        return a === b && (0 !== a || 1 / a === 1 / b) || a !== a && b !== b;
      }
      var He = "function" === typeof Object.is ? Object.is : Ge;
      function Ie(a, b) {
        if (He(a, b))
          return true;
        if ("object" !== typeof a || null === a || "object" !== typeof b || null === b)
          return false;
        var c = Object.keys(a), d = Object.keys(b);
        if (c.length !== d.length)
          return false;
        for (d = 0; d < c.length; d++) {
          var e = c[d];
          if (!ja.call(b, e) || !He(a[e], b[e]))
            return false;
        }
        return true;
      }
      function Je(a) {
        for (; a && a.firstChild; )
          a = a.firstChild;
        return a;
      }
      function Ke(a, b) {
        var c = Je(a);
        a = 0;
        for (var d; c; ) {
          if (3 === c.nodeType) {
            d = a + c.textContent.length;
            if (a <= b && d >= b)
              return { node: c, offset: b - a };
            a = d;
          }
          a: {
            for (; c; ) {
              if (c.nextSibling) {
                c = c.nextSibling;
                break a;
              }
              c = c.parentNode;
            }
            c = void 0;
          }
          c = Je(c);
        }
      }
      function Le(a, b) {
        return a && b ? a === b ? true : a && 3 === a.nodeType ? false : b && 3 === b.nodeType ? Le(a, b.parentNode) : "contains" in a ? a.contains(b) : a.compareDocumentPosition ? !!(a.compareDocumentPosition(b) & 16) : false : false;
      }
      function Me() {
        for (var a = window, b = Xa(); b instanceof a.HTMLIFrameElement; ) {
          try {
            var c = "string" === typeof b.contentWindow.location.href;
          } catch (d) {
            c = false;
          }
          if (c)
            a = b.contentWindow;
          else
            break;
          b = Xa(a.document);
        }
        return b;
      }
      function Ne(a) {
        var b = a && a.nodeName && a.nodeName.toLowerCase();
        return b && ("input" === b && ("text" === a.type || "search" === a.type || "tel" === a.type || "url" === a.type || "password" === a.type) || "textarea" === b || "true" === a.contentEditable);
      }
      function Oe(a) {
        var b = Me(), c = a.focusedElem, d = a.selectionRange;
        if (b !== c && c && c.ownerDocument && Le(c.ownerDocument.documentElement, c)) {
          if (null !== d && Ne(c)) {
            if (b = d.start, a = d.end, void 0 === a && (a = b), "selectionStart" in c)
              c.selectionStart = b, c.selectionEnd = Math.min(a, c.value.length);
            else if (a = (b = c.ownerDocument || document) && b.defaultView || window, a.getSelection) {
              a = a.getSelection();
              var e = c.textContent.length, f = Math.min(d.start, e);
              d = void 0 === d.end ? f : Math.min(d.end, e);
              !a.extend && f > d && (e = d, d = f, f = e);
              e = Ke(c, f);
              var g = Ke(
                c,
                d
              );
              e && g && (1 !== a.rangeCount || a.anchorNode !== e.node || a.anchorOffset !== e.offset || a.focusNode !== g.node || a.focusOffset !== g.offset) && (b = b.createRange(), b.setStart(e.node, e.offset), a.removeAllRanges(), f > d ? (a.addRange(b), a.extend(g.node, g.offset)) : (b.setEnd(g.node, g.offset), a.addRange(b)));
            }
          }
          b = [];
          for (a = c; a = a.parentNode; )
            1 === a.nodeType && b.push({ element: a, left: a.scrollLeft, top: a.scrollTop });
          "function" === typeof c.focus && c.focus();
          for (c = 0; c < b.length; c++)
            a = b[c], a.element.scrollLeft = a.left, a.element.scrollTop = a.top;
        }
      }
      var Pe = ia && "documentMode" in document && 11 >= document.documentMode;
      var Qe = null;
      var Re = null;
      var Se = null;
      var Te = false;
      function Ue(a, b, c) {
        var d = c.window === c ? c.document : 9 === c.nodeType ? c : c.ownerDocument;
        Te || null == Qe || Qe !== Xa(d) || (d = Qe, "selectionStart" in d && Ne(d) ? d = { start: d.selectionStart, end: d.selectionEnd } : (d = (d.ownerDocument && d.ownerDocument.defaultView || window).getSelection(), d = { anchorNode: d.anchorNode, anchorOffset: d.anchorOffset, focusNode: d.focusNode, focusOffset: d.focusOffset }), Se && Ie(Se, d) || (Se = d, d = oe(Re, "onSelect"), 0 < d.length && (b = new td("onSelect", "select", null, b, c), a.push({ event: b, listeners: d }), b.target = Qe)));
      }
      function Ve(a, b) {
        var c = {};
        c[a.toLowerCase()] = b.toLowerCase();
        c["Webkit" + a] = "webkit" + b;
        c["Moz" + a] = "moz" + b;
        return c;
      }
      var We = { animationend: Ve("Animation", "AnimationEnd"), animationiteration: Ve("Animation", "AnimationIteration"), animationstart: Ve("Animation", "AnimationStart"), transitionend: Ve("Transition", "TransitionEnd") };
      var Xe = {};
      var Ye = {};
      ia && (Ye = document.createElement("div").style, "AnimationEvent" in window || (delete We.animationend.animation, delete We.animationiteration.animation, delete We.animationstart.animation), "TransitionEvent" in window || delete We.transitionend.transition);
      function Ze(a) {
        if (Xe[a])
          return Xe[a];
        if (!We[a])
          return a;
        var b = We[a], c;
        for (c in b)
          if (b.hasOwnProperty(c) && c in Ye)
            return Xe[a] = b[c];
        return a;
      }
      var $e = Ze("animationend");
      var af = Ze("animationiteration");
      var bf = Ze("animationstart");
      var cf = Ze("transitionend");
      var df = /* @__PURE__ */ new Map();
      var ef = "abort auxClick cancel canPlay canPlayThrough click close contextMenu copy cut drag dragEnd dragEnter dragExit dragLeave dragOver dragStart drop durationChange emptied encrypted ended error gotPointerCapture input invalid keyDown keyPress keyUp load loadedData loadedMetadata loadStart lostPointerCapture mouseDown mouseMove mouseOut mouseOver mouseUp paste pause play playing pointerCancel pointerDown pointerMove pointerOut pointerOver pointerUp progress rateChange reset resize seeked seeking stalled submit suspend timeUpdate touchCancel touchEnd touchStart volumeChange scroll toggle touchMove waiting wheel".split(" ");
      function ff(a, b) {
        df.set(a, b);
        fa(b, [a]);
      }
      for (gf = 0; gf < ef.length; gf++) {
        hf = ef[gf], jf = hf.toLowerCase(), kf = hf[0].toUpperCase() + hf.slice(1);
        ff(jf, "on" + kf);
      }
      var hf;
      var jf;
      var kf;
      var gf;
      ff($e, "onAnimationEnd");
      ff(af, "onAnimationIteration");
      ff(bf, "onAnimationStart");
      ff("dblclick", "onDoubleClick");
      ff("focusin", "onFocus");
      ff("focusout", "onBlur");
      ff(cf, "onTransitionEnd");
      ha("onMouseEnter", ["mouseout", "mouseover"]);
      ha("onMouseLeave", ["mouseout", "mouseover"]);
      ha("onPointerEnter", ["pointerout", "pointerover"]);
      ha("onPointerLeave", ["pointerout", "pointerover"]);
      fa("onChange", "change click focusin focusout input keydown keyup selectionchange".split(" "));
      fa("onSelect", "focusout contextmenu dragend focusin keydown keyup mousedown mouseup selectionchange".split(" "));
      fa("onBeforeInput", ["compositionend", "keypress", "textInput", "paste"]);
      fa("onCompositionEnd", "compositionend focusout keydown keypress keyup mousedown".split(" "));
      fa("onCompositionStart", "compositionstart focusout keydown keypress keyup mousedown".split(" "));
      fa("onCompositionUpdate", "compositionupdate focusout keydown keypress keyup mousedown".split(" "));
      var lf = "abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange resize seeked seeking stalled suspend timeupdate volumechange waiting".split(" ");
      var mf = new Set("cancel close invalid load scroll toggle".split(" ").concat(lf));
      function nf(a, b, c) {
        var d = a.type || "unknown-event";
        a.currentTarget = c;
        Ub(d, b, void 0, a);
        a.currentTarget = null;
      }
      function se(a, b) {
        b = 0 !== (b & 4);
        for (var c = 0; c < a.length; c++) {
          var d = a[c], e = d.event;
          d = d.listeners;
          a: {
            var f = void 0;
            if (b)
              for (var g = d.length - 1; 0 <= g; g--) {
                var h = d[g], k = h.instance, l = h.currentTarget;
                h = h.listener;
                if (k !== f && e.isPropagationStopped())
                  break a;
                nf(e, h, l);
                f = k;
              }
            else
              for (g = 0; g < d.length; g++) {
                h = d[g];
                k = h.instance;
                l = h.currentTarget;
                h = h.listener;
                if (k !== f && e.isPropagationStopped())
                  break a;
                nf(e, h, l);
                f = k;
              }
          }
        }
        if (Qb)
          throw a = Rb, Qb = false, Rb = null, a;
      }
      function D(a, b) {
        var c = b[of];
        void 0 === c && (c = b[of] = /* @__PURE__ */ new Set());
        var d = a + "__bubble";
        c.has(d) || (pf(b, a, 2, false), c.add(d));
      }
      function qf(a, b, c) {
        var d = 0;
        b && (d |= 4);
        pf(c, a, d, b);
      }
      var rf = "_reactListening" + Math.random().toString(36).slice(2);
      function sf(a) {
        if (!a[rf]) {
          a[rf] = true;
          da.forEach(function(b2) {
            "selectionchange" !== b2 && (mf.has(b2) || qf(b2, false, a), qf(b2, true, a));
          });
          var b = 9 === a.nodeType ? a : a.ownerDocument;
          null === b || b[rf] || (b[rf] = true, qf("selectionchange", false, b));
        }
      }
      function pf(a, b, c, d) {
        switch (jd(b)) {
          case 1:
            var e = ed;
            break;
          case 4:
            e = gd;
            break;
          default:
            e = fd;
        }
        c = e.bind(null, b, c, a);
        e = void 0;
        !Lb || "touchstart" !== b && "touchmove" !== b && "wheel" !== b || (e = true);
        d ? void 0 !== e ? a.addEventListener(b, c, { capture: true, passive: e }) : a.addEventListener(b, c, true) : void 0 !== e ? a.addEventListener(b, c, { passive: e }) : a.addEventListener(b, c, false);
      }
      function hd(a, b, c, d, e) {
        var f = d;
        if (0 === (b & 1) && 0 === (b & 2) && null !== d)
          a:
            for (; ; ) {
              if (null === d)
                return;
              var g = d.tag;
              if (3 === g || 4 === g) {
                var h = d.stateNode.containerInfo;
                if (h === e || 8 === h.nodeType && h.parentNode === e)
                  break;
                if (4 === g)
                  for (g = d.return; null !== g; ) {
                    var k = g.tag;
                    if (3 === k || 4 === k) {
                      if (k = g.stateNode.containerInfo, k === e || 8 === k.nodeType && k.parentNode === e)
                        return;
                    }
                    g = g.return;
                  }
                for (; null !== h; ) {
                  g = Wc(h);
                  if (null === g)
                    return;
                  k = g.tag;
                  if (5 === k || 6 === k) {
                    d = f = g;
                    continue a;
                  }
                  h = h.parentNode;
                }
              }
              d = d.return;
            }
        Jb(function() {
          var d2 = f, e2 = xb(c), g2 = [];
          a: {
            var h2 = df.get(a);
            if (void 0 !== h2) {
              var k2 = td, n = a;
              switch (a) {
                case "keypress":
                  if (0 === od(c))
                    break a;
                case "keydown":
                case "keyup":
                  k2 = Rd;
                  break;
                case "focusin":
                  n = "focus";
                  k2 = Fd;
                  break;
                case "focusout":
                  n = "blur";
                  k2 = Fd;
                  break;
                case "beforeblur":
                case "afterblur":
                  k2 = Fd;
                  break;
                case "click":
                  if (2 === c.button)
                    break a;
                case "auxclick":
                case "dblclick":
                case "mousedown":
                case "mousemove":
                case "mouseup":
                case "mouseout":
                case "mouseover":
                case "contextmenu":
                  k2 = Bd;
                  break;
                case "drag":
                case "dragend":
                case "dragenter":
                case "dragexit":
                case "dragleave":
                case "dragover":
                case "dragstart":
                case "drop":
                  k2 = Dd;
                  break;
                case "touchcancel":
                case "touchend":
                case "touchmove":
                case "touchstart":
                  k2 = Vd;
                  break;
                case $e:
                case af:
                case bf:
                  k2 = Hd;
                  break;
                case cf:
                  k2 = Xd;
                  break;
                case "scroll":
                  k2 = vd;
                  break;
                case "wheel":
                  k2 = Zd;
                  break;
                case "copy":
                case "cut":
                case "paste":
                  k2 = Jd;
                  break;
                case "gotpointercapture":
                case "lostpointercapture":
                case "pointercancel":
                case "pointerdown":
                case "pointermove":
                case "pointerout":
                case "pointerover":
                case "pointerup":
                  k2 = Td;
              }
              var t = 0 !== (b & 4), J = !t && "scroll" === a, x = t ? null !== h2 ? h2 + "Capture" : null : h2;
              t = [];
              for (var w = d2, u; null !== w; ) {
                u = w;
                var F = u.stateNode;
                5 === u.tag && null !== F && (u = F, null !== x && (F = Kb(w, x), null != F && t.push(tf(w, F, u))));
                if (J)
                  break;
                w = w.return;
              }
              0 < t.length && (h2 = new k2(h2, n, null, c, e2), g2.push({ event: h2, listeners: t }));
            }
          }
          if (0 === (b & 7)) {
            a: {
              h2 = "mouseover" === a || "pointerover" === a;
              k2 = "mouseout" === a || "pointerout" === a;
              if (h2 && c !== wb && (n = c.relatedTarget || c.fromElement) && (Wc(n) || n[uf]))
                break a;
              if (k2 || h2) {
                h2 = e2.window === e2 ? e2 : (h2 = e2.ownerDocument) ? h2.defaultView || h2.parentWindow : window;
                if (k2) {
                  if (n = c.relatedTarget || c.toElement, k2 = d2, n = n ? Wc(n) : null, null !== n && (J = Vb(n), n !== J || 5 !== n.tag && 6 !== n.tag))
                    n = null;
                } else
                  k2 = null, n = d2;
                if (k2 !== n) {
                  t = Bd;
                  F = "onMouseLeave";
                  x = "onMouseEnter";
                  w = "mouse";
                  if ("pointerout" === a || "pointerover" === a)
                    t = Td, F = "onPointerLeave", x = "onPointerEnter", w = "pointer";
                  J = null == k2 ? h2 : ue(k2);
                  u = null == n ? h2 : ue(n);
                  h2 = new t(F, w + "leave", k2, c, e2);
                  h2.target = J;
                  h2.relatedTarget = u;
                  F = null;
                  Wc(e2) === d2 && (t = new t(x, w + "enter", n, c, e2), t.target = u, t.relatedTarget = J, F = t);
                  J = F;
                  if (k2 && n)
                    b: {
                      t = k2;
                      x = n;
                      w = 0;
                      for (u = t; u; u = vf(u))
                        w++;
                      u = 0;
                      for (F = x; F; F = vf(F))
                        u++;
                      for (; 0 < w - u; )
                        t = vf(t), w--;
                      for (; 0 < u - w; )
                        x = vf(x), u--;
                      for (; w--; ) {
                        if (t === x || null !== x && t === x.alternate)
                          break b;
                        t = vf(t);
                        x = vf(x);
                      }
                      t = null;
                    }
                  else
                    t = null;
                  null !== k2 && wf(g2, h2, k2, t, false);
                  null !== n && null !== J && wf(g2, J, n, t, true);
                }
              }
            }
            a: {
              h2 = d2 ? ue(d2) : window;
              k2 = h2.nodeName && h2.nodeName.toLowerCase();
              if ("select" === k2 || "input" === k2 && "file" === h2.type)
                var na = ve;
              else if (me(h2))
                if (we)
                  na = Fe;
                else {
                  na = De;
                  var xa = Ce;
                }
              else
                (k2 = h2.nodeName) && "input" === k2.toLowerCase() && ("checkbox" === h2.type || "radio" === h2.type) && (na = Ee);
              if (na && (na = na(a, d2))) {
                ne(g2, na, c, e2);
                break a;
              }
              xa && xa(a, h2, d2);
              "focusout" === a && (xa = h2._wrapperState) && xa.controlled && "number" === h2.type && cb(h2, "number", h2.value);
            }
            xa = d2 ? ue(d2) : window;
            switch (a) {
              case "focusin":
                if (me(xa) || "true" === xa.contentEditable)
                  Qe = xa, Re = d2, Se = null;
                break;
              case "focusout":
                Se = Re = Qe = null;
                break;
              case "mousedown":
                Te = true;
                break;
              case "contextmenu":
              case "mouseup":
              case "dragend":
                Te = false;
                Ue(g2, c, e2);
                break;
              case "selectionchange":
                if (Pe)
                  break;
              case "keydown":
              case "keyup":
                Ue(g2, c, e2);
            }
            var $a;
            if (ae)
              b: {
                switch (a) {
                  case "compositionstart":
                    var ba = "onCompositionStart";
                    break b;
                  case "compositionend":
                    ba = "onCompositionEnd";
                    break b;
                  case "compositionupdate":
                    ba = "onCompositionUpdate";
                    break b;
                }
                ba = void 0;
              }
            else
              ie ? ge(a, c) && (ba = "onCompositionEnd") : "keydown" === a && 229 === c.keyCode && (ba = "onCompositionStart");
            ba && (de && "ko" !== c.locale && (ie || "onCompositionStart" !== ba ? "onCompositionEnd" === ba && ie && ($a = nd()) : (kd = e2, ld = "value" in kd ? kd.value : kd.textContent, ie = true)), xa = oe(d2, ba), 0 < xa.length && (ba = new Ld(ba, a, null, c, e2), g2.push({ event: ba, listeners: xa }), $a ? ba.data = $a : ($a = he(c), null !== $a && (ba.data = $a))));
            if ($a = ce ? je(a, c) : ke(a, c))
              d2 = oe(d2, "onBeforeInput"), 0 < d2.length && (e2 = new Ld("onBeforeInput", "beforeinput", null, c, e2), g2.push({ event: e2, listeners: d2 }), e2.data = $a);
          }
          se(g2, b);
        });
      }
      function tf(a, b, c) {
        return { instance: a, listener: b, currentTarget: c };
      }
      function oe(a, b) {
        for (var c = b + "Capture", d = []; null !== a; ) {
          var e = a, f = e.stateNode;
          5 === e.tag && null !== f && (e = f, f = Kb(a, c), null != f && d.unshift(tf(a, f, e)), f = Kb(a, b), null != f && d.push(tf(a, f, e)));
          a = a.return;
        }
        return d;
      }
      function vf(a) {
        if (null === a)
          return null;
        do
          a = a.return;
        while (a && 5 !== a.tag);
        return a ? a : null;
      }
      function wf(a, b, c, d, e) {
        for (var f = b._reactName, g = []; null !== c && c !== d; ) {
          var h = c, k = h.alternate, l = h.stateNode;
          if (null !== k && k === d)
            break;
          5 === h.tag && null !== l && (h = l, e ? (k = Kb(c, f), null != k && g.unshift(tf(c, k, h))) : e || (k = Kb(c, f), null != k && g.push(tf(c, k, h))));
          c = c.return;
        }
        0 !== g.length && a.push({ event: b, listeners: g });
      }
      var xf = /\r\n?/g;
      var yf = /\u0000|\uFFFD/g;
      function zf(a) {
        return ("string" === typeof a ? a : "" + a).replace(xf, "\n").replace(yf, "");
      }
      function Af(a, b, c) {
        b = zf(b);
        if (zf(a) !== b && c)
          throw Error(p(425));
      }
      function Bf() {
      }
      var Cf = null;
      var Df = null;
      function Ef(a, b) {
        return "textarea" === a || "noscript" === a || "string" === typeof b.children || "number" === typeof b.children || "object" === typeof b.dangerouslySetInnerHTML && null !== b.dangerouslySetInnerHTML && null != b.dangerouslySetInnerHTML.__html;
      }
      var Ff = "function" === typeof setTimeout ? setTimeout : void 0;
      var Gf = "function" === typeof clearTimeout ? clearTimeout : void 0;
      var Hf = "function" === typeof Promise ? Promise : void 0;
      var Jf = "function" === typeof queueMicrotask ? queueMicrotask : "undefined" !== typeof Hf ? function(a) {
        return Hf.resolve(null).then(a).catch(If);
      } : Ff;
      function If(a) {
        setTimeout(function() {
          throw a;
        });
      }
      function Kf(a, b) {
        var c = b, d = 0;
        do {
          var e = c.nextSibling;
          a.removeChild(c);
          if (e && 8 === e.nodeType)
            if (c = e.data, "/$" === c) {
              if (0 === d) {
                a.removeChild(e);
                bd(b);
                return;
              }
              d--;
            } else
              "$" !== c && "$?" !== c && "$!" !== c || d++;
          c = e;
        } while (c);
        bd(b);
      }
      function Lf(a) {
        for (; null != a; a = a.nextSibling) {
          var b = a.nodeType;
          if (1 === b || 3 === b)
            break;
          if (8 === b) {
            b = a.data;
            if ("$" === b || "$!" === b || "$?" === b)
              break;
            if ("/$" === b)
              return null;
          }
        }
        return a;
      }
      function Mf(a) {
        a = a.previousSibling;
        for (var b = 0; a; ) {
          if (8 === a.nodeType) {
            var c = a.data;
            if ("$" === c || "$!" === c || "$?" === c) {
              if (0 === b)
                return a;
              b--;
            } else
              "/$" === c && b++;
          }
          a = a.previousSibling;
        }
        return null;
      }
      var Nf = Math.random().toString(36).slice(2);
      var Of = "__reactFiber$" + Nf;
      var Pf = "__reactProps$" + Nf;
      var uf = "__reactContainer$" + Nf;
      var of = "__reactEvents$" + Nf;
      var Qf = "__reactListeners$" + Nf;
      var Rf = "__reactHandles$" + Nf;
      function Wc(a) {
        var b = a[Of];
        if (b)
          return b;
        for (var c = a.parentNode; c; ) {
          if (b = c[uf] || c[Of]) {
            c = b.alternate;
            if (null !== b.child || null !== c && null !== c.child)
              for (a = Mf(a); null !== a; ) {
                if (c = a[Of])
                  return c;
                a = Mf(a);
              }
            return b;
          }
          a = c;
          c = a.parentNode;
        }
        return null;
      }
      function Cb(a) {
        a = a[Of] || a[uf];
        return !a || 5 !== a.tag && 6 !== a.tag && 13 !== a.tag && 3 !== a.tag ? null : a;
      }
      function ue(a) {
        if (5 === a.tag || 6 === a.tag)
          return a.stateNode;
        throw Error(p(33));
      }
      function Db(a) {
        return a[Pf] || null;
      }
      var Sf = [];
      var Tf = -1;
      function Uf(a) {
        return { current: a };
      }
      function E(a) {
        0 > Tf || (a.current = Sf[Tf], Sf[Tf] = null, Tf--);
      }
      function G4(a, b) {
        Tf++;
        Sf[Tf] = a.current;
        a.current = b;
      }
      var Vf = {};
      var H12 = Uf(Vf);
      var Wf = Uf(false);
      var Xf = Vf;
      function Yf(a, b) {
        var c = a.type.contextTypes;
        if (!c)
          return Vf;
        var d = a.stateNode;
        if (d && d.__reactInternalMemoizedUnmaskedChildContext === b)
          return d.__reactInternalMemoizedMaskedChildContext;
        var e = {}, f;
        for (f in c)
          e[f] = b[f];
        d && (a = a.stateNode, a.__reactInternalMemoizedUnmaskedChildContext = b, a.__reactInternalMemoizedMaskedChildContext = e);
        return e;
      }
      function Zf(a) {
        a = a.childContextTypes;
        return null !== a && void 0 !== a;
      }
      function $f() {
        E(Wf);
        E(H12);
      }
      function ag(a, b, c) {
        if (H12.current !== Vf)
          throw Error(p(168));
        G4(H12, b);
        G4(Wf, c);
      }
      function bg(a, b, c) {
        var d = a.stateNode;
        b = b.childContextTypes;
        if ("function" !== typeof d.getChildContext)
          return c;
        d = d.getChildContext();
        for (var e in d)
          if (!(e in b))
            throw Error(p(108, Ra(a) || "Unknown", e));
        return A({}, c, d);
      }
      function cg(a) {
        a = (a = a.stateNode) && a.__reactInternalMemoizedMergedChildContext || Vf;
        Xf = H12.current;
        G4(H12, a);
        G4(Wf, Wf.current);
        return true;
      }
      function dg(a, b, c) {
        var d = a.stateNode;
        if (!d)
          throw Error(p(169));
        c ? (a = bg(a, b, Xf), d.__reactInternalMemoizedMergedChildContext = a, E(Wf), E(H12), G4(H12, a)) : E(Wf);
        G4(Wf, c);
      }
      var eg = null;
      var fg = false;
      var gg = false;
      function hg(a) {
        null === eg ? eg = [a] : eg.push(a);
      }
      function ig(a) {
        fg = true;
        hg(a);
      }
      function jg() {
        if (!gg && null !== eg) {
          gg = true;
          var a = 0, b = C3;
          try {
            var c = eg;
            for (C3 = 1; a < c.length; a++) {
              var d = c[a];
              do
                d = d(true);
              while (null !== d);
            }
            eg = null;
            fg = false;
          } catch (e) {
            throw null !== eg && (eg = eg.slice(a + 1)), ac(fc, jg), e;
          } finally {
            C3 = b, gg = false;
          }
        }
        return null;
      }
      var kg = [];
      var lg = 0;
      var mg = null;
      var ng = 0;
      var og = [];
      var pg = 0;
      var qg = null;
      var rg = 1;
      var sg = "";
      function tg(a, b) {
        kg[lg++] = ng;
        kg[lg++] = mg;
        mg = a;
        ng = b;
      }
      function ug(a, b, c) {
        og[pg++] = rg;
        og[pg++] = sg;
        og[pg++] = qg;
        qg = a;
        var d = rg;
        a = sg;
        var e = 32 - oc(d) - 1;
        d &= ~(1 << e);
        c += 1;
        var f = 32 - oc(b) + e;
        if (30 < f) {
          var g = e - e % 5;
          f = (d & (1 << g) - 1).toString(32);
          d >>= g;
          e -= g;
          rg = 1 << 32 - oc(b) + e | c << e | d;
          sg = f + a;
        } else
          rg = 1 << f | c << e | d, sg = a;
      }
      function vg(a) {
        null !== a.return && (tg(a, 1), ug(a, 1, 0));
      }
      function wg(a) {
        for (; a === mg; )
          mg = kg[--lg], kg[lg] = null, ng = kg[--lg], kg[lg] = null;
        for (; a === qg; )
          qg = og[--pg], og[pg] = null, sg = og[--pg], og[pg] = null, rg = og[--pg], og[pg] = null;
      }
      var xg = null;
      var yg = null;
      var I = false;
      var zg = null;
      function Ag(a, b) {
        var c = Bg(5, null, null, 0);
        c.elementType = "DELETED";
        c.stateNode = b;
        c.return = a;
        b = a.deletions;
        null === b ? (a.deletions = [c], a.flags |= 16) : b.push(c);
      }
      function Cg(a, b) {
        switch (a.tag) {
          case 5:
            var c = a.type;
            b = 1 !== b.nodeType || c.toLowerCase() !== b.nodeName.toLowerCase() ? null : b;
            return null !== b ? (a.stateNode = b, xg = a, yg = Lf(b.firstChild), true) : false;
          case 6:
            return b = "" === a.pendingProps || 3 !== b.nodeType ? null : b, null !== b ? (a.stateNode = b, xg = a, yg = null, true) : false;
          case 13:
            return b = 8 !== b.nodeType ? null : b, null !== b ? (c = null !== qg ? { id: rg, overflow: sg } : null, a.memoizedState = { dehydrated: b, treeContext: c, retryLane: 1073741824 }, c = Bg(18, null, null, 0), c.stateNode = b, c.return = a, a.child = c, xg = a, yg = null, true) : false;
          default:
            return false;
        }
      }
      function Dg(a) {
        return 0 !== (a.mode & 1) && 0 === (a.flags & 128);
      }
      function Eg(a) {
        if (I) {
          var b = yg;
          if (b) {
            var c = b;
            if (!Cg(a, b)) {
              if (Dg(a))
                throw Error(p(418));
              b = Lf(c.nextSibling);
              var d = xg;
              b && Cg(a, b) ? Ag(d, c) : (a.flags = a.flags & -4097 | 2, I = false, xg = a);
            }
          } else {
            if (Dg(a))
              throw Error(p(418));
            a.flags = a.flags & -4097 | 2;
            I = false;
            xg = a;
          }
        }
      }
      function Fg(a) {
        for (a = a.return; null !== a && 5 !== a.tag && 3 !== a.tag && 13 !== a.tag; )
          a = a.return;
        xg = a;
      }
      function Gg(a) {
        if (a !== xg)
          return false;
        if (!I)
          return Fg(a), I = true, false;
        var b;
        (b = 3 !== a.tag) && !(b = 5 !== a.tag) && (b = a.type, b = "head" !== b && "body" !== b && !Ef(a.type, a.memoizedProps));
        if (b && (b = yg)) {
          if (Dg(a))
            throw Hg(), Error(p(418));
          for (; b; )
            Ag(a, b), b = Lf(b.nextSibling);
        }
        Fg(a);
        if (13 === a.tag) {
          a = a.memoizedState;
          a = null !== a ? a.dehydrated : null;
          if (!a)
            throw Error(p(317));
          a: {
            a = a.nextSibling;
            for (b = 0; a; ) {
              if (8 === a.nodeType) {
                var c = a.data;
                if ("/$" === c) {
                  if (0 === b) {
                    yg = Lf(a.nextSibling);
                    break a;
                  }
                  b--;
                } else
                  "$" !== c && "$!" !== c && "$?" !== c || b++;
              }
              a = a.nextSibling;
            }
            yg = null;
          }
        } else
          yg = xg ? Lf(a.stateNode.nextSibling) : null;
        return true;
      }
      function Hg() {
        for (var a = yg; a; )
          a = Lf(a.nextSibling);
      }
      function Ig() {
        yg = xg = null;
        I = false;
      }
      function Jg(a) {
        null === zg ? zg = [a] : zg.push(a);
      }
      var Kg = ua.ReactCurrentBatchConfig;
      function Lg(a, b, c) {
        a = c.ref;
        if (null !== a && "function" !== typeof a && "object" !== typeof a) {
          if (c._owner) {
            c = c._owner;
            if (c) {
              if (1 !== c.tag)
                throw Error(p(309));
              var d = c.stateNode;
            }
            if (!d)
              throw Error(p(147, a));
            var e = d, f = "" + a;
            if (null !== b && null !== b.ref && "function" === typeof b.ref && b.ref._stringRef === f)
              return b.ref;
            b = function(a2) {
              var b2 = e.refs;
              null === a2 ? delete b2[f] : b2[f] = a2;
            };
            b._stringRef = f;
            return b;
          }
          if ("string" !== typeof a)
            throw Error(p(284));
          if (!c._owner)
            throw Error(p(290, a));
        }
        return a;
      }
      function Mg(a, b) {
        a = Object.prototype.toString.call(b);
        throw Error(p(31, "[object Object]" === a ? "object with keys {" + Object.keys(b).join(", ") + "}" : a));
      }
      function Ng(a) {
        var b = a._init;
        return b(a._payload);
      }
      function Og(a) {
        function b(b2, c2) {
          if (a) {
            var d2 = b2.deletions;
            null === d2 ? (b2.deletions = [c2], b2.flags |= 16) : d2.push(c2);
          }
        }
        function c(c2, d2) {
          if (!a)
            return null;
          for (; null !== d2; )
            b(c2, d2), d2 = d2.sibling;
          return null;
        }
        function d(a2, b2) {
          for (a2 = /* @__PURE__ */ new Map(); null !== b2; )
            null !== b2.key ? a2.set(b2.key, b2) : a2.set(b2.index, b2), b2 = b2.sibling;
          return a2;
        }
        function e(a2, b2) {
          a2 = Pg(a2, b2);
          a2.index = 0;
          a2.sibling = null;
          return a2;
        }
        function f(b2, c2, d2) {
          b2.index = d2;
          if (!a)
            return b2.flags |= 1048576, c2;
          d2 = b2.alternate;
          if (null !== d2)
            return d2 = d2.index, d2 < c2 ? (b2.flags |= 2, c2) : d2;
          b2.flags |= 2;
          return c2;
        }
        function g(b2) {
          a && null === b2.alternate && (b2.flags |= 2);
          return b2;
        }
        function h(a2, b2, c2, d2) {
          if (null === b2 || 6 !== b2.tag)
            return b2 = Qg(c2, a2.mode, d2), b2.return = a2, b2;
          b2 = e(b2, c2);
          b2.return = a2;
          return b2;
        }
        function k(a2, b2, c2, d2) {
          var f2 = c2.type;
          if (f2 === ya)
            return m(a2, b2, c2.props.children, d2, c2.key);
          if (null !== b2 && (b2.elementType === f2 || "object" === typeof f2 && null !== f2 && f2.$$typeof === Ha && Ng(f2) === b2.type))
            return d2 = e(b2, c2.props), d2.ref = Lg(a2, b2, c2), d2.return = a2, d2;
          d2 = Rg(c2.type, c2.key, c2.props, null, a2.mode, d2);
          d2.ref = Lg(a2, b2, c2);
          d2.return = a2;
          return d2;
        }
        function l(a2, b2, c2, d2) {
          if (null === b2 || 4 !== b2.tag || b2.stateNode.containerInfo !== c2.containerInfo || b2.stateNode.implementation !== c2.implementation)
            return b2 = Sg(c2, a2.mode, d2), b2.return = a2, b2;
          b2 = e(b2, c2.children || []);
          b2.return = a2;
          return b2;
        }
        function m(a2, b2, c2, d2, f2) {
          if (null === b2 || 7 !== b2.tag)
            return b2 = Tg(c2, a2.mode, d2, f2), b2.return = a2, b2;
          b2 = e(b2, c2);
          b2.return = a2;
          return b2;
        }
        function q(a2, b2, c2) {
          if ("string" === typeof b2 && "" !== b2 || "number" === typeof b2)
            return b2 = Qg("" + b2, a2.mode, c2), b2.return = a2, b2;
          if ("object" === typeof b2 && null !== b2) {
            switch (b2.$$typeof) {
              case va:
                return c2 = Rg(b2.type, b2.key, b2.props, null, a2.mode, c2), c2.ref = Lg(a2, null, b2), c2.return = a2, c2;
              case wa:
                return b2 = Sg(b2, a2.mode, c2), b2.return = a2, b2;
              case Ha:
                var d2 = b2._init;
                return q(a2, d2(b2._payload), c2);
            }
            if (eb(b2) || Ka(b2))
              return b2 = Tg(b2, a2.mode, c2, null), b2.return = a2, b2;
            Mg(a2, b2);
          }
          return null;
        }
        function r(a2, b2, c2, d2) {
          var e2 = null !== b2 ? b2.key : null;
          if ("string" === typeof c2 && "" !== c2 || "number" === typeof c2)
            return null !== e2 ? null : h(a2, b2, "" + c2, d2);
          if ("object" === typeof c2 && null !== c2) {
            switch (c2.$$typeof) {
              case va:
                return c2.key === e2 ? k(a2, b2, c2, d2) : null;
              case wa:
                return c2.key === e2 ? l(a2, b2, c2, d2) : null;
              case Ha:
                return e2 = c2._init, r(
                  a2,
                  b2,
                  e2(c2._payload),
                  d2
                );
            }
            if (eb(c2) || Ka(c2))
              return null !== e2 ? null : m(a2, b2, c2, d2, null);
            Mg(a2, c2);
          }
          return null;
        }
        function y(a2, b2, c2, d2, e2) {
          if ("string" === typeof d2 && "" !== d2 || "number" === typeof d2)
            return a2 = a2.get(c2) || null, h(b2, a2, "" + d2, e2);
          if ("object" === typeof d2 && null !== d2) {
            switch (d2.$$typeof) {
              case va:
                return a2 = a2.get(null === d2.key ? c2 : d2.key) || null, k(b2, a2, d2, e2);
              case wa:
                return a2 = a2.get(null === d2.key ? c2 : d2.key) || null, l(b2, a2, d2, e2);
              case Ha:
                var f2 = d2._init;
                return y(a2, b2, c2, f2(d2._payload), e2);
            }
            if (eb(d2) || Ka(d2))
              return a2 = a2.get(c2) || null, m(b2, a2, d2, e2, null);
            Mg(b2, d2);
          }
          return null;
        }
        function n(e2, g2, h2, k2) {
          for (var l2 = null, m2 = null, u = g2, w = g2 = 0, x = null; null !== u && w < h2.length; w++) {
            u.index > w ? (x = u, u = null) : x = u.sibling;
            var n2 = r(e2, u, h2[w], k2);
            if (null === n2) {
              null === u && (u = x);
              break;
            }
            a && u && null === n2.alternate && b(e2, u);
            g2 = f(n2, g2, w);
            null === m2 ? l2 = n2 : m2.sibling = n2;
            m2 = n2;
            u = x;
          }
          if (w === h2.length)
            return c(e2, u), I && tg(e2, w), l2;
          if (null === u) {
            for (; w < h2.length; w++)
              u = q(e2, h2[w], k2), null !== u && (g2 = f(u, g2, w), null === m2 ? l2 = u : m2.sibling = u, m2 = u);
            I && tg(e2, w);
            return l2;
          }
          for (u = d(e2, u); w < h2.length; w++)
            x = y(u, e2, w, h2[w], k2), null !== x && (a && null !== x.alternate && u.delete(null === x.key ? w : x.key), g2 = f(x, g2, w), null === m2 ? l2 = x : m2.sibling = x, m2 = x);
          a && u.forEach(function(a2) {
            return b(e2, a2);
          });
          I && tg(e2, w);
          return l2;
        }
        function t(e2, g2, h2, k2) {
          var l2 = Ka(h2);
          if ("function" !== typeof l2)
            throw Error(p(150));
          h2 = l2.call(h2);
          if (null == h2)
            throw Error(p(151));
          for (var u = l2 = null, m2 = g2, w = g2 = 0, x = null, n2 = h2.next(); null !== m2 && !n2.done; w++, n2 = h2.next()) {
            m2.index > w ? (x = m2, m2 = null) : x = m2.sibling;
            var t2 = r(e2, m2, n2.value, k2);
            if (null === t2) {
              null === m2 && (m2 = x);
              break;
            }
            a && m2 && null === t2.alternate && b(e2, m2);
            g2 = f(t2, g2, w);
            null === u ? l2 = t2 : u.sibling = t2;
            u = t2;
            m2 = x;
          }
          if (n2.done)
            return c(
              e2,
              m2
            ), I && tg(e2, w), l2;
          if (null === m2) {
            for (; !n2.done; w++, n2 = h2.next())
              n2 = q(e2, n2.value, k2), null !== n2 && (g2 = f(n2, g2, w), null === u ? l2 = n2 : u.sibling = n2, u = n2);
            I && tg(e2, w);
            return l2;
          }
          for (m2 = d(e2, m2); !n2.done; w++, n2 = h2.next())
            n2 = y(m2, e2, w, n2.value, k2), null !== n2 && (a && null !== n2.alternate && m2.delete(null === n2.key ? w : n2.key), g2 = f(n2, g2, w), null === u ? l2 = n2 : u.sibling = n2, u = n2);
          a && m2.forEach(function(a2) {
            return b(e2, a2);
          });
          I && tg(e2, w);
          return l2;
        }
        function J(a2, d2, f2, h2) {
          "object" === typeof f2 && null !== f2 && f2.type === ya && null === f2.key && (f2 = f2.props.children);
          if ("object" === typeof f2 && null !== f2) {
            switch (f2.$$typeof) {
              case va:
                a: {
                  for (var k2 = f2.key, l2 = d2; null !== l2; ) {
                    if (l2.key === k2) {
                      k2 = f2.type;
                      if (k2 === ya) {
                        if (7 === l2.tag) {
                          c(a2, l2.sibling);
                          d2 = e(l2, f2.props.children);
                          d2.return = a2;
                          a2 = d2;
                          break a;
                        }
                      } else if (l2.elementType === k2 || "object" === typeof k2 && null !== k2 && k2.$$typeof === Ha && Ng(k2) === l2.type) {
                        c(a2, l2.sibling);
                        d2 = e(l2, f2.props);
                        d2.ref = Lg(a2, l2, f2);
                        d2.return = a2;
                        a2 = d2;
                        break a;
                      }
                      c(a2, l2);
                      break;
                    } else
                      b(a2, l2);
                    l2 = l2.sibling;
                  }
                  f2.type === ya ? (d2 = Tg(f2.props.children, a2.mode, h2, f2.key), d2.return = a2, a2 = d2) : (h2 = Rg(f2.type, f2.key, f2.props, null, a2.mode, h2), h2.ref = Lg(a2, d2, f2), h2.return = a2, a2 = h2);
                }
                return g(a2);
              case wa:
                a: {
                  for (l2 = f2.key; null !== d2; ) {
                    if (d2.key === l2)
                      if (4 === d2.tag && d2.stateNode.containerInfo === f2.containerInfo && d2.stateNode.implementation === f2.implementation) {
                        c(a2, d2.sibling);
                        d2 = e(d2, f2.children || []);
                        d2.return = a2;
                        a2 = d2;
                        break a;
                      } else {
                        c(a2, d2);
                        break;
                      }
                    else
                      b(a2, d2);
                    d2 = d2.sibling;
                  }
                  d2 = Sg(f2, a2.mode, h2);
                  d2.return = a2;
                  a2 = d2;
                }
                return g(a2);
              case Ha:
                return l2 = f2._init, J(a2, d2, l2(f2._payload), h2);
            }
            if (eb(f2))
              return n(a2, d2, f2, h2);
            if (Ka(f2))
              return t(a2, d2, f2, h2);
            Mg(a2, f2);
          }
          return "string" === typeof f2 && "" !== f2 || "number" === typeof f2 ? (f2 = "" + f2, null !== d2 && 6 === d2.tag ? (c(a2, d2.sibling), d2 = e(d2, f2), d2.return = a2, a2 = d2) : (c(a2, d2), d2 = Qg(f2, a2.mode, h2), d2.return = a2, a2 = d2), g(a2)) : c(a2, d2);
        }
        return J;
      }
      var Ug = Og(true);
      var Vg = Og(false);
      var Wg = Uf(null);
      var Xg = null;
      var Yg = null;
      var Zg = null;
      function $g() {
        Zg = Yg = Xg = null;
      }
      function ah(a) {
        var b = Wg.current;
        E(Wg);
        a._currentValue = b;
      }
      function bh(a, b, c) {
        for (; null !== a; ) {
          var d = a.alternate;
          (a.childLanes & b) !== b ? (a.childLanes |= b, null !== d && (d.childLanes |= b)) : null !== d && (d.childLanes & b) !== b && (d.childLanes |= b);
          if (a === c)
            break;
          a = a.return;
        }
      }
      function ch(a, b) {
        Xg = a;
        Zg = Yg = null;
        a = a.dependencies;
        null !== a && null !== a.firstContext && (0 !== (a.lanes & b) && (dh = true), a.firstContext = null);
      }
      function eh(a) {
        var b = a._currentValue;
        if (Zg !== a)
          if (a = { context: a, memoizedValue: b, next: null }, null === Yg) {
            if (null === Xg)
              throw Error(p(308));
            Yg = a;
            Xg.dependencies = { lanes: 0, firstContext: a };
          } else
            Yg = Yg.next = a;
        return b;
      }
      var fh = null;
      function gh(a) {
        null === fh ? fh = [a] : fh.push(a);
      }
      function hh(a, b, c, d) {
        var e = b.interleaved;
        null === e ? (c.next = c, gh(b)) : (c.next = e.next, e.next = c);
        b.interleaved = c;
        return ih(a, d);
      }
      function ih(a, b) {
        a.lanes |= b;
        var c = a.alternate;
        null !== c && (c.lanes |= b);
        c = a;
        for (a = a.return; null !== a; )
          a.childLanes |= b, c = a.alternate, null !== c && (c.childLanes |= b), c = a, a = a.return;
        return 3 === c.tag ? c.stateNode : null;
      }
      var jh = false;
      function kh(a) {
        a.updateQueue = { baseState: a.memoizedState, firstBaseUpdate: null, lastBaseUpdate: null, shared: { pending: null, interleaved: null, lanes: 0 }, effects: null };
      }
      function lh(a, b) {
        a = a.updateQueue;
        b.updateQueue === a && (b.updateQueue = { baseState: a.baseState, firstBaseUpdate: a.firstBaseUpdate, lastBaseUpdate: a.lastBaseUpdate, shared: a.shared, effects: a.effects });
      }
      function mh(a, b) {
        return { eventTime: a, lane: b, tag: 0, payload: null, callback: null, next: null };
      }
      function nh(a, b, c) {
        var d = a.updateQueue;
        if (null === d)
          return null;
        d = d.shared;
        if (0 !== (K & 2)) {
          var e = d.pending;
          null === e ? b.next = b : (b.next = e.next, e.next = b);
          d.pending = b;
          return ih(a, c);
        }
        e = d.interleaved;
        null === e ? (b.next = b, gh(d)) : (b.next = e.next, e.next = b);
        d.interleaved = b;
        return ih(a, c);
      }
      function oh(a, b, c) {
        b = b.updateQueue;
        if (null !== b && (b = b.shared, 0 !== (c & 4194240))) {
          var d = b.lanes;
          d &= a.pendingLanes;
          c |= d;
          b.lanes = c;
          Cc(a, c);
        }
      }
      function ph(a, b) {
        var c = a.updateQueue, d = a.alternate;
        if (null !== d && (d = d.updateQueue, c === d)) {
          var e = null, f = null;
          c = c.firstBaseUpdate;
          if (null !== c) {
            do {
              var g = { eventTime: c.eventTime, lane: c.lane, tag: c.tag, payload: c.payload, callback: c.callback, next: null };
              null === f ? e = f = g : f = f.next = g;
              c = c.next;
            } while (null !== c);
            null === f ? e = f = b : f = f.next = b;
          } else
            e = f = b;
          c = { baseState: d.baseState, firstBaseUpdate: e, lastBaseUpdate: f, shared: d.shared, effects: d.effects };
          a.updateQueue = c;
          return;
        }
        a = c.lastBaseUpdate;
        null === a ? c.firstBaseUpdate = b : a.next = b;
        c.lastBaseUpdate = b;
      }
      function qh(a, b, c, d) {
        var e = a.updateQueue;
        jh = false;
        var f = e.firstBaseUpdate, g = e.lastBaseUpdate, h = e.shared.pending;
        if (null !== h) {
          e.shared.pending = null;
          var k = h, l = k.next;
          k.next = null;
          null === g ? f = l : g.next = l;
          g = k;
          var m = a.alternate;
          null !== m && (m = m.updateQueue, h = m.lastBaseUpdate, h !== g && (null === h ? m.firstBaseUpdate = l : h.next = l, m.lastBaseUpdate = k));
        }
        if (null !== f) {
          var q = e.baseState;
          g = 0;
          m = l = k = null;
          h = f;
          do {
            var r = h.lane, y = h.eventTime;
            if ((d & r) === r) {
              null !== m && (m = m.next = {
                eventTime: y,
                lane: 0,
                tag: h.tag,
                payload: h.payload,
                callback: h.callback,
                next: null
              });
              a: {
                var n = a, t = h;
                r = b;
                y = c;
                switch (t.tag) {
                  case 1:
                    n = t.payload;
                    if ("function" === typeof n) {
                      q = n.call(y, q, r);
                      break a;
                    }
                    q = n;
                    break a;
                  case 3:
                    n.flags = n.flags & -65537 | 128;
                  case 0:
                    n = t.payload;
                    r = "function" === typeof n ? n.call(y, q, r) : n;
                    if (null === r || void 0 === r)
                      break a;
                    q = A({}, q, r);
                    break a;
                  case 2:
                    jh = true;
                }
              }
              null !== h.callback && 0 !== h.lane && (a.flags |= 64, r = e.effects, null === r ? e.effects = [h] : r.push(h));
            } else
              y = { eventTime: y, lane: r, tag: h.tag, payload: h.payload, callback: h.callback, next: null }, null === m ? (l = m = y, k = q) : m = m.next = y, g |= r;
            h = h.next;
            if (null === h)
              if (h = e.shared.pending, null === h)
                break;
              else
                r = h, h = r.next, r.next = null, e.lastBaseUpdate = r, e.shared.pending = null;
          } while (1);
          null === m && (k = q);
          e.baseState = k;
          e.firstBaseUpdate = l;
          e.lastBaseUpdate = m;
          b = e.shared.interleaved;
          if (null !== b) {
            e = b;
            do
              g |= e.lane, e = e.next;
            while (e !== b);
          } else
            null === f && (e.shared.lanes = 0);
          rh |= g;
          a.lanes = g;
          a.memoizedState = q;
        }
      }
      function sh(a, b, c) {
        a = b.effects;
        b.effects = null;
        if (null !== a)
          for (b = 0; b < a.length; b++) {
            var d = a[b], e = d.callback;
            if (null !== e) {
              d.callback = null;
              d = c;
              if ("function" !== typeof e)
                throw Error(p(191, e));
              e.call(d);
            }
          }
      }
      var th = {};
      var uh = Uf(th);
      var vh = Uf(th);
      var wh = Uf(th);
      function xh(a) {
        if (a === th)
          throw Error(p(174));
        return a;
      }
      function yh(a, b) {
        G4(wh, b);
        G4(vh, a);
        G4(uh, th);
        a = b.nodeType;
        switch (a) {
          case 9:
          case 11:
            b = (b = b.documentElement) ? b.namespaceURI : lb(null, "");
            break;
          default:
            a = 8 === a ? b.parentNode : b, b = a.namespaceURI || null, a = a.tagName, b = lb(b, a);
        }
        E(uh);
        G4(uh, b);
      }
      function zh() {
        E(uh);
        E(vh);
        E(wh);
      }
      function Ah(a) {
        xh(wh.current);
        var b = xh(uh.current);
        var c = lb(b, a.type);
        b !== c && (G4(vh, a), G4(uh, c));
      }
      function Bh(a) {
        vh.current === a && (E(uh), E(vh));
      }
      var L = Uf(0);
      function Ch(a) {
        for (var b = a; null !== b; ) {
          if (13 === b.tag) {
            var c = b.memoizedState;
            if (null !== c && (c = c.dehydrated, null === c || "$?" === c.data || "$!" === c.data))
              return b;
          } else if (19 === b.tag && void 0 !== b.memoizedProps.revealOrder) {
            if (0 !== (b.flags & 128))
              return b;
          } else if (null !== b.child) {
            b.child.return = b;
            b = b.child;
            continue;
          }
          if (b === a)
            break;
          for (; null === b.sibling; ) {
            if (null === b.return || b.return === a)
              return null;
            b = b.return;
          }
          b.sibling.return = b.return;
          b = b.sibling;
        }
        return null;
      }
      var Dh = [];
      function Eh() {
        for (var a = 0; a < Dh.length; a++)
          Dh[a]._workInProgressVersionPrimary = null;
        Dh.length = 0;
      }
      var Fh = ua.ReactCurrentDispatcher;
      var Gh = ua.ReactCurrentBatchConfig;
      var Hh = 0;
      var M = null;
      var N = null;
      var O2 = null;
      var Ih = false;
      var Jh = false;
      var Kh = 0;
      var Lh = 0;
      function P() {
        throw Error(p(321));
      }
      function Mh(a, b) {
        if (null === b)
          return false;
        for (var c = 0; c < b.length && c < a.length; c++)
          if (!He(a[c], b[c]))
            return false;
        return true;
      }
      function Nh(a, b, c, d, e, f) {
        Hh = f;
        M = b;
        b.memoizedState = null;
        b.updateQueue = null;
        b.lanes = 0;
        Fh.current = null === a || null === a.memoizedState ? Oh : Ph;
        a = c(d, e);
        if (Jh) {
          f = 0;
          do {
            Jh = false;
            Kh = 0;
            if (25 <= f)
              throw Error(p(301));
            f += 1;
            O2 = N = null;
            b.updateQueue = null;
            Fh.current = Qh;
            a = c(d, e);
          } while (Jh);
        }
        Fh.current = Rh;
        b = null !== N && null !== N.next;
        Hh = 0;
        O2 = N = M = null;
        Ih = false;
        if (b)
          throw Error(p(300));
        return a;
      }
      function Sh() {
        var a = 0 !== Kh;
        Kh = 0;
        return a;
      }
      function Th() {
        var a = { memoizedState: null, baseState: null, baseQueue: null, queue: null, next: null };
        null === O2 ? M.memoizedState = O2 = a : O2 = O2.next = a;
        return O2;
      }
      function Uh() {
        if (null === N) {
          var a = M.alternate;
          a = null !== a ? a.memoizedState : null;
        } else
          a = N.next;
        var b = null === O2 ? M.memoizedState : O2.next;
        if (null !== b)
          O2 = b, N = a;
        else {
          if (null === a)
            throw Error(p(310));
          N = a;
          a = { memoizedState: N.memoizedState, baseState: N.baseState, baseQueue: N.baseQueue, queue: N.queue, next: null };
          null === O2 ? M.memoizedState = O2 = a : O2 = O2.next = a;
        }
        return O2;
      }
      function Vh(a, b) {
        return "function" === typeof b ? b(a) : b;
      }
      function Wh(a) {
        var b = Uh(), c = b.queue;
        if (null === c)
          throw Error(p(311));
        c.lastRenderedReducer = a;
        var d = N, e = d.baseQueue, f = c.pending;
        if (null !== f) {
          if (null !== e) {
            var g = e.next;
            e.next = f.next;
            f.next = g;
          }
          d.baseQueue = e = f;
          c.pending = null;
        }
        if (null !== e) {
          f = e.next;
          d = d.baseState;
          var h = g = null, k = null, l = f;
          do {
            var m = l.lane;
            if ((Hh & m) === m)
              null !== k && (k = k.next = { lane: 0, action: l.action, hasEagerState: l.hasEagerState, eagerState: l.eagerState, next: null }), d = l.hasEagerState ? l.eagerState : a(d, l.action);
            else {
              var q = {
                lane: m,
                action: l.action,
                hasEagerState: l.hasEagerState,
                eagerState: l.eagerState,
                next: null
              };
              null === k ? (h = k = q, g = d) : k = k.next = q;
              M.lanes |= m;
              rh |= m;
            }
            l = l.next;
          } while (null !== l && l !== f);
          null === k ? g = d : k.next = h;
          He(d, b.memoizedState) || (dh = true);
          b.memoizedState = d;
          b.baseState = g;
          b.baseQueue = k;
          c.lastRenderedState = d;
        }
        a = c.interleaved;
        if (null !== a) {
          e = a;
          do
            f = e.lane, M.lanes |= f, rh |= f, e = e.next;
          while (e !== a);
        } else
          null === e && (c.lanes = 0);
        return [b.memoizedState, c.dispatch];
      }
      function Xh(a) {
        var b = Uh(), c = b.queue;
        if (null === c)
          throw Error(p(311));
        c.lastRenderedReducer = a;
        var d = c.dispatch, e = c.pending, f = b.memoizedState;
        if (null !== e) {
          c.pending = null;
          var g = e = e.next;
          do
            f = a(f, g.action), g = g.next;
          while (g !== e);
          He(f, b.memoizedState) || (dh = true);
          b.memoizedState = f;
          null === b.baseQueue && (b.baseState = f);
          c.lastRenderedState = f;
        }
        return [f, d];
      }
      function Yh() {
      }
      function Zh(a, b) {
        var c = M, d = Uh(), e = b(), f = !He(d.memoizedState, e);
        f && (d.memoizedState = e, dh = true);
        d = d.queue;
        $h(ai.bind(null, c, d, a), [a]);
        if (d.getSnapshot !== b || f || null !== O2 && O2.memoizedState.tag & 1) {
          c.flags |= 2048;
          bi(9, ci.bind(null, c, d, e, b), void 0, null);
          if (null === Q)
            throw Error(p(349));
          0 !== (Hh & 30) || di(c, b, e);
        }
        return e;
      }
      function di(a, b, c) {
        a.flags |= 16384;
        a = { getSnapshot: b, value: c };
        b = M.updateQueue;
        null === b ? (b = { lastEffect: null, stores: null }, M.updateQueue = b, b.stores = [a]) : (c = b.stores, null === c ? b.stores = [a] : c.push(a));
      }
      function ci(a, b, c, d) {
        b.value = c;
        b.getSnapshot = d;
        ei(b) && fi(a);
      }
      function ai(a, b, c) {
        return c(function() {
          ei(b) && fi(a);
        });
      }
      function ei(a) {
        var b = a.getSnapshot;
        a = a.value;
        try {
          var c = b();
          return !He(a, c);
        } catch (d) {
          return true;
        }
      }
      function fi(a) {
        var b = ih(a, 1);
        null !== b && gi(b, a, 1, -1);
      }
      function hi(a) {
        var b = Th();
        "function" === typeof a && (a = a());
        b.memoizedState = b.baseState = a;
        a = { pending: null, interleaved: null, lanes: 0, dispatch: null, lastRenderedReducer: Vh, lastRenderedState: a };
        b.queue = a;
        a = a.dispatch = ii.bind(null, M, a);
        return [b.memoizedState, a];
      }
      function bi(a, b, c, d) {
        a = { tag: a, create: b, destroy: c, deps: d, next: null };
        b = M.updateQueue;
        null === b ? (b = { lastEffect: null, stores: null }, M.updateQueue = b, b.lastEffect = a.next = a) : (c = b.lastEffect, null === c ? b.lastEffect = a.next = a : (d = c.next, c.next = a, a.next = d, b.lastEffect = a));
        return a;
      }
      function ji() {
        return Uh().memoizedState;
      }
      function ki(a, b, c, d) {
        var e = Th();
        M.flags |= a;
        e.memoizedState = bi(1 | b, c, void 0, void 0 === d ? null : d);
      }
      function li(a, b, c, d) {
        var e = Uh();
        d = void 0 === d ? null : d;
        var f = void 0;
        if (null !== N) {
          var g = N.memoizedState;
          f = g.destroy;
          if (null !== d && Mh(d, g.deps)) {
            e.memoizedState = bi(b, c, f, d);
            return;
          }
        }
        M.flags |= a;
        e.memoizedState = bi(1 | b, c, f, d);
      }
      function mi(a, b) {
        return ki(8390656, 8, a, b);
      }
      function $h(a, b) {
        return li(2048, 8, a, b);
      }
      function ni(a, b) {
        return li(4, 2, a, b);
      }
      function oi(a, b) {
        return li(4, 4, a, b);
      }
      function pi(a, b) {
        if ("function" === typeof b)
          return a = a(), b(a), function() {
            b(null);
          };
        if (null !== b && void 0 !== b)
          return a = a(), b.current = a, function() {
            b.current = null;
          };
      }
      function qi(a, b, c) {
        c = null !== c && void 0 !== c ? c.concat([a]) : null;
        return li(4, 4, pi.bind(null, b, a), c);
      }
      function ri() {
      }
      function si(a, b) {
        var c = Uh();
        b = void 0 === b ? null : b;
        var d = c.memoizedState;
        if (null !== d && null !== b && Mh(b, d[1]))
          return d[0];
        c.memoizedState = [a, b];
        return a;
      }
      function ti(a, b) {
        var c = Uh();
        b = void 0 === b ? null : b;
        var d = c.memoizedState;
        if (null !== d && null !== b && Mh(b, d[1]))
          return d[0];
        a = a();
        c.memoizedState = [a, b];
        return a;
      }
      function ui(a, b, c) {
        if (0 === (Hh & 21))
          return a.baseState && (a.baseState = false, dh = true), a.memoizedState = c;
        He(c, b) || (c = yc(), M.lanes |= c, rh |= c, a.baseState = true);
        return b;
      }
      function vi(a, b) {
        var c = C3;
        C3 = 0 !== c && 4 > c ? c : 4;
        a(true);
        var d = Gh.transition;
        Gh.transition = {};
        try {
          a(false), b();
        } finally {
          C3 = c, Gh.transition = d;
        }
      }
      function wi() {
        return Uh().memoizedState;
      }
      function xi(a, b, c) {
        var d = yi(a);
        c = { lane: d, action: c, hasEagerState: false, eagerState: null, next: null };
        if (zi(a))
          Ai(b, c);
        else if (c = hh(a, b, c, d), null !== c) {
          var e = R();
          gi(c, a, d, e);
          Bi(c, b, d);
        }
      }
      function ii(a, b, c) {
        var d = yi(a), e = { lane: d, action: c, hasEagerState: false, eagerState: null, next: null };
        if (zi(a))
          Ai(b, e);
        else {
          var f = a.alternate;
          if (0 === a.lanes && (null === f || 0 === f.lanes) && (f = b.lastRenderedReducer, null !== f))
            try {
              var g = b.lastRenderedState, h = f(g, c);
              e.hasEagerState = true;
              e.eagerState = h;
              if (He(h, g)) {
                var k = b.interleaved;
                null === k ? (e.next = e, gh(b)) : (e.next = k.next, k.next = e);
                b.interleaved = e;
                return;
              }
            } catch (l) {
            } finally {
            }
          c = hh(a, b, e, d);
          null !== c && (e = R(), gi(c, a, d, e), Bi(c, b, d));
        }
      }
      function zi(a) {
        var b = a.alternate;
        return a === M || null !== b && b === M;
      }
      function Ai(a, b) {
        Jh = Ih = true;
        var c = a.pending;
        null === c ? b.next = b : (b.next = c.next, c.next = b);
        a.pending = b;
      }
      function Bi(a, b, c) {
        if (0 !== (c & 4194240)) {
          var d = b.lanes;
          d &= a.pendingLanes;
          c |= d;
          b.lanes = c;
          Cc(a, c);
        }
      }
      var Rh = { readContext: eh, useCallback: P, useContext: P, useEffect: P, useImperativeHandle: P, useInsertionEffect: P, useLayoutEffect: P, useMemo: P, useReducer: P, useRef: P, useState: P, useDebugValue: P, useDeferredValue: P, useTransition: P, useMutableSource: P, useSyncExternalStore: P, useId: P, unstable_isNewReconciler: false };
      var Oh = { readContext: eh, useCallback: function(a, b) {
        Th().memoizedState = [a, void 0 === b ? null : b];
        return a;
      }, useContext: eh, useEffect: mi, useImperativeHandle: function(a, b, c) {
        c = null !== c && void 0 !== c ? c.concat([a]) : null;
        return ki(
          4194308,
          4,
          pi.bind(null, b, a),
          c
        );
      }, useLayoutEffect: function(a, b) {
        return ki(4194308, 4, a, b);
      }, useInsertionEffect: function(a, b) {
        return ki(4, 2, a, b);
      }, useMemo: function(a, b) {
        var c = Th();
        b = void 0 === b ? null : b;
        a = a();
        c.memoizedState = [a, b];
        return a;
      }, useReducer: function(a, b, c) {
        var d = Th();
        b = void 0 !== c ? c(b) : b;
        d.memoizedState = d.baseState = b;
        a = { pending: null, interleaved: null, lanes: 0, dispatch: null, lastRenderedReducer: a, lastRenderedState: b };
        d.queue = a;
        a = a.dispatch = xi.bind(null, M, a);
        return [d.memoizedState, a];
      }, useRef: function(a) {
        var b = Th();
        a = { current: a };
        return b.memoizedState = a;
      }, useState: hi, useDebugValue: ri, useDeferredValue: function(a) {
        return Th().memoizedState = a;
      }, useTransition: function() {
        var a = hi(false), b = a[0];
        a = vi.bind(null, a[1]);
        Th().memoizedState = a;
        return [b, a];
      }, useMutableSource: function() {
      }, useSyncExternalStore: function(a, b, c) {
        var d = M, e = Th();
        if (I) {
          if (void 0 === c)
            throw Error(p(407));
          c = c();
        } else {
          c = b();
          if (null === Q)
            throw Error(p(349));
          0 !== (Hh & 30) || di(d, b, c);
        }
        e.memoizedState = c;
        var f = { value: c, getSnapshot: b };
        e.queue = f;
        mi(ai.bind(
          null,
          d,
          f,
          a
        ), [a]);
        d.flags |= 2048;
        bi(9, ci.bind(null, d, f, c, b), void 0, null);
        return c;
      }, useId: function() {
        var a = Th(), b = Q.identifierPrefix;
        if (I) {
          var c = sg;
          var d = rg;
          c = (d & ~(1 << 32 - oc(d) - 1)).toString(32) + c;
          b = ":" + b + "R" + c;
          c = Kh++;
          0 < c && (b += "H" + c.toString(32));
          b += ":";
        } else
          c = Lh++, b = ":" + b + "r" + c.toString(32) + ":";
        return a.memoizedState = b;
      }, unstable_isNewReconciler: false };
      var Ph = {
        readContext: eh,
        useCallback: si,
        useContext: eh,
        useEffect: $h,
        useImperativeHandle: qi,
        useInsertionEffect: ni,
        useLayoutEffect: oi,
        useMemo: ti,
        useReducer: Wh,
        useRef: ji,
        useState: function() {
          return Wh(Vh);
        },
        useDebugValue: ri,
        useDeferredValue: function(a) {
          var b = Uh();
          return ui(b, N.memoizedState, a);
        },
        useTransition: function() {
          var a = Wh(Vh)[0], b = Uh().memoizedState;
          return [a, b];
        },
        useMutableSource: Yh,
        useSyncExternalStore: Zh,
        useId: wi,
        unstable_isNewReconciler: false
      };
      var Qh = { readContext: eh, useCallback: si, useContext: eh, useEffect: $h, useImperativeHandle: qi, useInsertionEffect: ni, useLayoutEffect: oi, useMemo: ti, useReducer: Xh, useRef: ji, useState: function() {
        return Xh(Vh);
      }, useDebugValue: ri, useDeferredValue: function(a) {
        var b = Uh();
        return null === N ? b.memoizedState = a : ui(b, N.memoizedState, a);
      }, useTransition: function() {
        var a = Xh(Vh)[0], b = Uh().memoizedState;
        return [a, b];
      }, useMutableSource: Yh, useSyncExternalStore: Zh, useId: wi, unstable_isNewReconciler: false };
      function Ci(a, b) {
        if (a && a.defaultProps) {
          b = A({}, b);
          a = a.defaultProps;
          for (var c in a)
            void 0 === b[c] && (b[c] = a[c]);
          return b;
        }
        return b;
      }
      function Di(a, b, c, d) {
        b = a.memoizedState;
        c = c(d, b);
        c = null === c || void 0 === c ? b : A({}, b, c);
        a.memoizedState = c;
        0 === a.lanes && (a.updateQueue.baseState = c);
      }
      var Ei = { isMounted: function(a) {
        return (a = a._reactInternals) ? Vb(a) === a : false;
      }, enqueueSetState: function(a, b, c) {
        a = a._reactInternals;
        var d = R(), e = yi(a), f = mh(d, e);
        f.payload = b;
        void 0 !== c && null !== c && (f.callback = c);
        b = nh(a, f, e);
        null !== b && (gi(b, a, e, d), oh(b, a, e));
      }, enqueueReplaceState: function(a, b, c) {
        a = a._reactInternals;
        var d = R(), e = yi(a), f = mh(d, e);
        f.tag = 1;
        f.payload = b;
        void 0 !== c && null !== c && (f.callback = c);
        b = nh(a, f, e);
        null !== b && (gi(b, a, e, d), oh(b, a, e));
      }, enqueueForceUpdate: function(a, b) {
        a = a._reactInternals;
        var c = R(), d = yi(a), e = mh(c, d);
        e.tag = 2;
        void 0 !== b && null !== b && (e.callback = b);
        b = nh(a, e, d);
        null !== b && (gi(b, a, d, c), oh(b, a, d));
      } };
      function Fi(a, b, c, d, e, f, g) {
        a = a.stateNode;
        return "function" === typeof a.shouldComponentUpdate ? a.shouldComponentUpdate(d, f, g) : b.prototype && b.prototype.isPureReactComponent ? !Ie(c, d) || !Ie(e, f) : true;
      }
      function Gi(a, b, c) {
        var d = false, e = Vf;
        var f = b.contextType;
        "object" === typeof f && null !== f ? f = eh(f) : (e = Zf(b) ? Xf : H12.current, d = b.contextTypes, f = (d = null !== d && void 0 !== d) ? Yf(a, e) : Vf);
        b = new b(c, f);
        a.memoizedState = null !== b.state && void 0 !== b.state ? b.state : null;
        b.updater = Ei;
        a.stateNode = b;
        b._reactInternals = a;
        d && (a = a.stateNode, a.__reactInternalMemoizedUnmaskedChildContext = e, a.__reactInternalMemoizedMaskedChildContext = f);
        return b;
      }
      function Hi(a, b, c, d) {
        a = b.state;
        "function" === typeof b.componentWillReceiveProps && b.componentWillReceiveProps(c, d);
        "function" === typeof b.UNSAFE_componentWillReceiveProps && b.UNSAFE_componentWillReceiveProps(c, d);
        b.state !== a && Ei.enqueueReplaceState(b, b.state, null);
      }
      function Ii(a, b, c, d) {
        var e = a.stateNode;
        e.props = c;
        e.state = a.memoizedState;
        e.refs = {};
        kh(a);
        var f = b.contextType;
        "object" === typeof f && null !== f ? e.context = eh(f) : (f = Zf(b) ? Xf : H12.current, e.context = Yf(a, f));
        e.state = a.memoizedState;
        f = b.getDerivedStateFromProps;
        "function" === typeof f && (Di(a, b, f, c), e.state = a.memoizedState);
        "function" === typeof b.getDerivedStateFromProps || "function" === typeof e.getSnapshotBeforeUpdate || "function" !== typeof e.UNSAFE_componentWillMount && "function" !== typeof e.componentWillMount || (b = e.state, "function" === typeof e.componentWillMount && e.componentWillMount(), "function" === typeof e.UNSAFE_componentWillMount && e.UNSAFE_componentWillMount(), b !== e.state && Ei.enqueueReplaceState(e, e.state, null), qh(a, c, e, d), e.state = a.memoizedState);
        "function" === typeof e.componentDidMount && (a.flags |= 4194308);
      }
      function Ji(a, b) {
        try {
          var c = "", d = b;
          do
            c += Pa(d), d = d.return;
          while (d);
          var e = c;
        } catch (f) {
          e = "\nError generating stack: " + f.message + "\n" + f.stack;
        }
        return { value: a, source: b, stack: e, digest: null };
      }
      function Ki(a, b, c) {
        return { value: a, source: null, stack: null != c ? c : null, digest: null != b ? b : null };
      }
      function Li(a, b) {
        try {
          console.error(b.value);
        } catch (c) {
          setTimeout(function() {
            throw c;
          });
        }
      }
      var Mi = "function" === typeof WeakMap ? WeakMap : Map;
      function Ni(a, b, c) {
        c = mh(-1, c);
        c.tag = 3;
        c.payload = { element: null };
        var d = b.value;
        c.callback = function() {
          Oi || (Oi = true, Pi = d);
          Li(a, b);
        };
        return c;
      }
      function Qi(a, b, c) {
        c = mh(-1, c);
        c.tag = 3;
        var d = a.type.getDerivedStateFromError;
        if ("function" === typeof d) {
          var e = b.value;
          c.payload = function() {
            return d(e);
          };
          c.callback = function() {
            Li(a, b);
          };
        }
        var f = a.stateNode;
        null !== f && "function" === typeof f.componentDidCatch && (c.callback = function() {
          Li(a, b);
          "function" !== typeof d && (null === Ri ? Ri = /* @__PURE__ */ new Set([this]) : Ri.add(this));
          var c2 = b.stack;
          this.componentDidCatch(b.value, { componentStack: null !== c2 ? c2 : "" });
        });
        return c;
      }
      function Si(a, b, c) {
        var d = a.pingCache;
        if (null === d) {
          d = a.pingCache = new Mi();
          var e = /* @__PURE__ */ new Set();
          d.set(b, e);
        } else
          e = d.get(b), void 0 === e && (e = /* @__PURE__ */ new Set(), d.set(b, e));
        e.has(c) || (e.add(c), a = Ti.bind(null, a, b, c), b.then(a, a));
      }
      function Ui(a) {
        do {
          var b;
          if (b = 13 === a.tag)
            b = a.memoizedState, b = null !== b ? null !== b.dehydrated ? true : false : true;
          if (b)
            return a;
          a = a.return;
        } while (null !== a);
        return null;
      }
      function Vi(a, b, c, d, e) {
        if (0 === (a.mode & 1))
          return a === b ? a.flags |= 65536 : (a.flags |= 128, c.flags |= 131072, c.flags &= -52805, 1 === c.tag && (null === c.alternate ? c.tag = 17 : (b = mh(-1, 1), b.tag = 2, nh(c, b, 1))), c.lanes |= 1), a;
        a.flags |= 65536;
        a.lanes = e;
        return a;
      }
      var Wi = ua.ReactCurrentOwner;
      var dh = false;
      function Xi(a, b, c, d) {
        b.child = null === a ? Vg(b, null, c, d) : Ug(b, a.child, c, d);
      }
      function Yi(a, b, c, d, e) {
        c = c.render;
        var f = b.ref;
        ch(b, e);
        d = Nh(a, b, c, d, f, e);
        c = Sh();
        if (null !== a && !dh)
          return b.updateQueue = a.updateQueue, b.flags &= -2053, a.lanes &= ~e, Zi(a, b, e);
        I && c && vg(b);
        b.flags |= 1;
        Xi(a, b, d, e);
        return b.child;
      }
      function $i(a, b, c, d, e) {
        if (null === a) {
          var f = c.type;
          if ("function" === typeof f && !aj(f) && void 0 === f.defaultProps && null === c.compare && void 0 === c.defaultProps)
            return b.tag = 15, b.type = f, bj(a, b, f, d, e);
          a = Rg(c.type, null, d, b, b.mode, e);
          a.ref = b.ref;
          a.return = b;
          return b.child = a;
        }
        f = a.child;
        if (0 === (a.lanes & e)) {
          var g = f.memoizedProps;
          c = c.compare;
          c = null !== c ? c : Ie;
          if (c(g, d) && a.ref === b.ref)
            return Zi(a, b, e);
        }
        b.flags |= 1;
        a = Pg(f, d);
        a.ref = b.ref;
        a.return = b;
        return b.child = a;
      }
      function bj(a, b, c, d, e) {
        if (null !== a) {
          var f = a.memoizedProps;
          if (Ie(f, d) && a.ref === b.ref)
            if (dh = false, b.pendingProps = d = f, 0 !== (a.lanes & e))
              0 !== (a.flags & 131072) && (dh = true);
            else
              return b.lanes = a.lanes, Zi(a, b, e);
        }
        return cj(a, b, c, d, e);
      }
      function dj(a, b, c) {
        var d = b.pendingProps, e = d.children, f = null !== a ? a.memoizedState : null;
        if ("hidden" === d.mode)
          if (0 === (b.mode & 1))
            b.memoizedState = { baseLanes: 0, cachePool: null, transitions: null }, G4(ej, fj), fj |= c;
          else {
            if (0 === (c & 1073741824))
              return a = null !== f ? f.baseLanes | c : c, b.lanes = b.childLanes = 1073741824, b.memoizedState = { baseLanes: a, cachePool: null, transitions: null }, b.updateQueue = null, G4(ej, fj), fj |= a, null;
            b.memoizedState = { baseLanes: 0, cachePool: null, transitions: null };
            d = null !== f ? f.baseLanes : c;
            G4(ej, fj);
            fj |= d;
          }
        else
          null !== f ? (d = f.baseLanes | c, b.memoizedState = null) : d = c, G4(ej, fj), fj |= d;
        Xi(a, b, e, c);
        return b.child;
      }
      function gj(a, b) {
        var c = b.ref;
        if (null === a && null !== c || null !== a && a.ref !== c)
          b.flags |= 512, b.flags |= 2097152;
      }
      function cj(a, b, c, d, e) {
        var f = Zf(c) ? Xf : H12.current;
        f = Yf(b, f);
        ch(b, e);
        c = Nh(a, b, c, d, f, e);
        d = Sh();
        if (null !== a && !dh)
          return b.updateQueue = a.updateQueue, b.flags &= -2053, a.lanes &= ~e, Zi(a, b, e);
        I && d && vg(b);
        b.flags |= 1;
        Xi(a, b, c, e);
        return b.child;
      }
      function hj(a, b, c, d, e) {
        if (Zf(c)) {
          var f = true;
          cg(b);
        } else
          f = false;
        ch(b, e);
        if (null === b.stateNode)
          ij(a, b), Gi(b, c, d), Ii(b, c, d, e), d = true;
        else if (null === a) {
          var g = b.stateNode, h = b.memoizedProps;
          g.props = h;
          var k = g.context, l = c.contextType;
          "object" === typeof l && null !== l ? l = eh(l) : (l = Zf(c) ? Xf : H12.current, l = Yf(b, l));
          var m = c.getDerivedStateFromProps, q = "function" === typeof m || "function" === typeof g.getSnapshotBeforeUpdate;
          q || "function" !== typeof g.UNSAFE_componentWillReceiveProps && "function" !== typeof g.componentWillReceiveProps || (h !== d || k !== l) && Hi(b, g, d, l);
          jh = false;
          var r = b.memoizedState;
          g.state = r;
          qh(b, d, g, e);
          k = b.memoizedState;
          h !== d || r !== k || Wf.current || jh ? ("function" === typeof m && (Di(b, c, m, d), k = b.memoizedState), (h = jh || Fi(b, c, h, d, r, k, l)) ? (q || "function" !== typeof g.UNSAFE_componentWillMount && "function" !== typeof g.componentWillMount || ("function" === typeof g.componentWillMount && g.componentWillMount(), "function" === typeof g.UNSAFE_componentWillMount && g.UNSAFE_componentWillMount()), "function" === typeof g.componentDidMount && (b.flags |= 4194308)) : ("function" === typeof g.componentDidMount && (b.flags |= 4194308), b.memoizedProps = d, b.memoizedState = k), g.props = d, g.state = k, g.context = l, d = h) : ("function" === typeof g.componentDidMount && (b.flags |= 4194308), d = false);
        } else {
          g = b.stateNode;
          lh(a, b);
          h = b.memoizedProps;
          l = b.type === b.elementType ? h : Ci(b.type, h);
          g.props = l;
          q = b.pendingProps;
          r = g.context;
          k = c.contextType;
          "object" === typeof k && null !== k ? k = eh(k) : (k = Zf(c) ? Xf : H12.current, k = Yf(b, k));
          var y = c.getDerivedStateFromProps;
          (m = "function" === typeof y || "function" === typeof g.getSnapshotBeforeUpdate) || "function" !== typeof g.UNSAFE_componentWillReceiveProps && "function" !== typeof g.componentWillReceiveProps || (h !== q || r !== k) && Hi(b, g, d, k);
          jh = false;
          r = b.memoizedState;
          g.state = r;
          qh(b, d, g, e);
          var n = b.memoizedState;
          h !== q || r !== n || Wf.current || jh ? ("function" === typeof y && (Di(b, c, y, d), n = b.memoizedState), (l = jh || Fi(b, c, l, d, r, n, k) || false) ? (m || "function" !== typeof g.UNSAFE_componentWillUpdate && "function" !== typeof g.componentWillUpdate || ("function" === typeof g.componentWillUpdate && g.componentWillUpdate(d, n, k), "function" === typeof g.UNSAFE_componentWillUpdate && g.UNSAFE_componentWillUpdate(d, n, k)), "function" === typeof g.componentDidUpdate && (b.flags |= 4), "function" === typeof g.getSnapshotBeforeUpdate && (b.flags |= 1024)) : ("function" !== typeof g.componentDidUpdate || h === a.memoizedProps && r === a.memoizedState || (b.flags |= 4), "function" !== typeof g.getSnapshotBeforeUpdate || h === a.memoizedProps && r === a.memoizedState || (b.flags |= 1024), b.memoizedProps = d, b.memoizedState = n), g.props = d, g.state = n, g.context = k, d = l) : ("function" !== typeof g.componentDidUpdate || h === a.memoizedProps && r === a.memoizedState || (b.flags |= 4), "function" !== typeof g.getSnapshotBeforeUpdate || h === a.memoizedProps && r === a.memoizedState || (b.flags |= 1024), d = false);
        }
        return jj(a, b, c, d, f, e);
      }
      function jj(a, b, c, d, e, f) {
        gj(a, b);
        var g = 0 !== (b.flags & 128);
        if (!d && !g)
          return e && dg(b, c, false), Zi(a, b, f);
        d = b.stateNode;
        Wi.current = b;
        var h = g && "function" !== typeof c.getDerivedStateFromError ? null : d.render();
        b.flags |= 1;
        null !== a && g ? (b.child = Ug(b, a.child, null, f), b.child = Ug(b, null, h, f)) : Xi(a, b, h, f);
        b.memoizedState = d.state;
        e && dg(b, c, true);
        return b.child;
      }
      function kj(a) {
        var b = a.stateNode;
        b.pendingContext ? ag(a, b.pendingContext, b.pendingContext !== b.context) : b.context && ag(a, b.context, false);
        yh(a, b.containerInfo);
      }
      function lj(a, b, c, d, e) {
        Ig();
        Jg(e);
        b.flags |= 256;
        Xi(a, b, c, d);
        return b.child;
      }
      var mj = { dehydrated: null, treeContext: null, retryLane: 0 };
      function nj(a) {
        return { baseLanes: a, cachePool: null, transitions: null };
      }
      function oj(a, b, c) {
        var d = b.pendingProps, e = L.current, f = false, g = 0 !== (b.flags & 128), h;
        (h = g) || (h = null !== a && null === a.memoizedState ? false : 0 !== (e & 2));
        if (h)
          f = true, b.flags &= -129;
        else if (null === a || null !== a.memoizedState)
          e |= 1;
        G4(L, e & 1);
        if (null === a) {
          Eg(b);
          a = b.memoizedState;
          if (null !== a && (a = a.dehydrated, null !== a))
            return 0 === (b.mode & 1) ? b.lanes = 1 : "$!" === a.data ? b.lanes = 8 : b.lanes = 1073741824, null;
          g = d.children;
          a = d.fallback;
          return f ? (d = b.mode, f = b.child, g = { mode: "hidden", children: g }, 0 === (d & 1) && null !== f ? (f.childLanes = 0, f.pendingProps = g) : f = pj(g, d, 0, null), a = Tg(a, d, c, null), f.return = b, a.return = b, f.sibling = a, b.child = f, b.child.memoizedState = nj(c), b.memoizedState = mj, a) : qj(b, g);
        }
        e = a.memoizedState;
        if (null !== e && (h = e.dehydrated, null !== h))
          return rj(a, b, g, d, h, e, c);
        if (f) {
          f = d.fallback;
          g = b.mode;
          e = a.child;
          h = e.sibling;
          var k = { mode: "hidden", children: d.children };
          0 === (g & 1) && b.child !== e ? (d = b.child, d.childLanes = 0, d.pendingProps = k, b.deletions = null) : (d = Pg(e, k), d.subtreeFlags = e.subtreeFlags & 14680064);
          null !== h ? f = Pg(h, f) : (f = Tg(f, g, c, null), f.flags |= 2);
          f.return = b;
          d.return = b;
          d.sibling = f;
          b.child = d;
          d = f;
          f = b.child;
          g = a.child.memoizedState;
          g = null === g ? nj(c) : { baseLanes: g.baseLanes | c, cachePool: null, transitions: g.transitions };
          f.memoizedState = g;
          f.childLanes = a.childLanes & ~c;
          b.memoizedState = mj;
          return d;
        }
        f = a.child;
        a = f.sibling;
        d = Pg(f, { mode: "visible", children: d.children });
        0 === (b.mode & 1) && (d.lanes = c);
        d.return = b;
        d.sibling = null;
        null !== a && (c = b.deletions, null === c ? (b.deletions = [a], b.flags |= 16) : c.push(a));
        b.child = d;
        b.memoizedState = null;
        return d;
      }
      function qj(a, b) {
        b = pj({ mode: "visible", children: b }, a.mode, 0, null);
        b.return = a;
        return a.child = b;
      }
      function sj(a, b, c, d) {
        null !== d && Jg(d);
        Ug(b, a.child, null, c);
        a = qj(b, b.pendingProps.children);
        a.flags |= 2;
        b.memoizedState = null;
        return a;
      }
      function rj(a, b, c, d, e, f, g) {
        if (c) {
          if (b.flags & 256)
            return b.flags &= -257, d = Ki(Error(p(422))), sj(a, b, g, d);
          if (null !== b.memoizedState)
            return b.child = a.child, b.flags |= 128, null;
          f = d.fallback;
          e = b.mode;
          d = pj({ mode: "visible", children: d.children }, e, 0, null);
          f = Tg(f, e, g, null);
          f.flags |= 2;
          d.return = b;
          f.return = b;
          d.sibling = f;
          b.child = d;
          0 !== (b.mode & 1) && Ug(b, a.child, null, g);
          b.child.memoizedState = nj(g);
          b.memoizedState = mj;
          return f;
        }
        if (0 === (b.mode & 1))
          return sj(a, b, g, null);
        if ("$!" === e.data) {
          d = e.nextSibling && e.nextSibling.dataset;
          if (d)
            var h = d.dgst;
          d = h;
          f = Error(p(419));
          d = Ki(f, d, void 0);
          return sj(a, b, g, d);
        }
        h = 0 !== (g & a.childLanes);
        if (dh || h) {
          d = Q;
          if (null !== d) {
            switch (g & -g) {
              case 4:
                e = 2;
                break;
              case 16:
                e = 8;
                break;
              case 64:
              case 128:
              case 256:
              case 512:
              case 1024:
              case 2048:
              case 4096:
              case 8192:
              case 16384:
              case 32768:
              case 65536:
              case 131072:
              case 262144:
              case 524288:
              case 1048576:
              case 2097152:
              case 4194304:
              case 8388608:
              case 16777216:
              case 33554432:
              case 67108864:
                e = 32;
                break;
              case 536870912:
                e = 268435456;
                break;
              default:
                e = 0;
            }
            e = 0 !== (e & (d.suspendedLanes | g)) ? 0 : e;
            0 !== e && e !== f.retryLane && (f.retryLane = e, ih(a, e), gi(d, a, e, -1));
          }
          tj();
          d = Ki(Error(p(421)));
          return sj(a, b, g, d);
        }
        if ("$?" === e.data)
          return b.flags |= 128, b.child = a.child, b = uj.bind(null, a), e._reactRetry = b, null;
        a = f.treeContext;
        yg = Lf(e.nextSibling);
        xg = b;
        I = true;
        zg = null;
        null !== a && (og[pg++] = rg, og[pg++] = sg, og[pg++] = qg, rg = a.id, sg = a.overflow, qg = b);
        b = qj(b, d.children);
        b.flags |= 4096;
        return b;
      }
      function vj(a, b, c) {
        a.lanes |= b;
        var d = a.alternate;
        null !== d && (d.lanes |= b);
        bh(a.return, b, c);
      }
      function wj(a, b, c, d, e) {
        var f = a.memoizedState;
        null === f ? a.memoizedState = { isBackwards: b, rendering: null, renderingStartTime: 0, last: d, tail: c, tailMode: e } : (f.isBackwards = b, f.rendering = null, f.renderingStartTime = 0, f.last = d, f.tail = c, f.tailMode = e);
      }
      function xj(a, b, c) {
        var d = b.pendingProps, e = d.revealOrder, f = d.tail;
        Xi(a, b, d.children, c);
        d = L.current;
        if (0 !== (d & 2))
          d = d & 1 | 2, b.flags |= 128;
        else {
          if (null !== a && 0 !== (a.flags & 128))
            a:
              for (a = b.child; null !== a; ) {
                if (13 === a.tag)
                  null !== a.memoizedState && vj(a, c, b);
                else if (19 === a.tag)
                  vj(a, c, b);
                else if (null !== a.child) {
                  a.child.return = a;
                  a = a.child;
                  continue;
                }
                if (a === b)
                  break a;
                for (; null === a.sibling; ) {
                  if (null === a.return || a.return === b)
                    break a;
                  a = a.return;
                }
                a.sibling.return = a.return;
                a = a.sibling;
              }
          d &= 1;
        }
        G4(L, d);
        if (0 === (b.mode & 1))
          b.memoizedState = null;
        else
          switch (e) {
            case "forwards":
              c = b.child;
              for (e = null; null !== c; )
                a = c.alternate, null !== a && null === Ch(a) && (e = c), c = c.sibling;
              c = e;
              null === c ? (e = b.child, b.child = null) : (e = c.sibling, c.sibling = null);
              wj(b, false, e, c, f);
              break;
            case "backwards":
              c = null;
              e = b.child;
              for (b.child = null; null !== e; ) {
                a = e.alternate;
                if (null !== a && null === Ch(a)) {
                  b.child = e;
                  break;
                }
                a = e.sibling;
                e.sibling = c;
                c = e;
                e = a;
              }
              wj(b, true, c, null, f);
              break;
            case "together":
              wj(b, false, null, null, void 0);
              break;
            default:
              b.memoizedState = null;
          }
        return b.child;
      }
      function ij(a, b) {
        0 === (b.mode & 1) && null !== a && (a.alternate = null, b.alternate = null, b.flags |= 2);
      }
      function Zi(a, b, c) {
        null !== a && (b.dependencies = a.dependencies);
        rh |= b.lanes;
        if (0 === (c & b.childLanes))
          return null;
        if (null !== a && b.child !== a.child)
          throw Error(p(153));
        if (null !== b.child) {
          a = b.child;
          c = Pg(a, a.pendingProps);
          b.child = c;
          for (c.return = b; null !== a.sibling; )
            a = a.sibling, c = c.sibling = Pg(a, a.pendingProps), c.return = b;
          c.sibling = null;
        }
        return b.child;
      }
      function yj(a, b, c) {
        switch (b.tag) {
          case 3:
            kj(b);
            Ig();
            break;
          case 5:
            Ah(b);
            break;
          case 1:
            Zf(b.type) && cg(b);
            break;
          case 4:
            yh(b, b.stateNode.containerInfo);
            break;
          case 10:
            var d = b.type._context, e = b.memoizedProps.value;
            G4(Wg, d._currentValue);
            d._currentValue = e;
            break;
          case 13:
            d = b.memoizedState;
            if (null !== d) {
              if (null !== d.dehydrated)
                return G4(L, L.current & 1), b.flags |= 128, null;
              if (0 !== (c & b.child.childLanes))
                return oj(a, b, c);
              G4(L, L.current & 1);
              a = Zi(a, b, c);
              return null !== a ? a.sibling : null;
            }
            G4(L, L.current & 1);
            break;
          case 19:
            d = 0 !== (c & b.childLanes);
            if (0 !== (a.flags & 128)) {
              if (d)
                return xj(a, b, c);
              b.flags |= 128;
            }
            e = b.memoizedState;
            null !== e && (e.rendering = null, e.tail = null, e.lastEffect = null);
            G4(L, L.current);
            if (d)
              break;
            else
              return null;
          case 22:
          case 23:
            return b.lanes = 0, dj(a, b, c);
        }
        return Zi(a, b, c);
      }
      var zj;
      var Aj;
      var Bj;
      var Cj;
      zj = function(a, b) {
        for (var c = b.child; null !== c; ) {
          if (5 === c.tag || 6 === c.tag)
            a.appendChild(c.stateNode);
          else if (4 !== c.tag && null !== c.child) {
            c.child.return = c;
            c = c.child;
            continue;
          }
          if (c === b)
            break;
          for (; null === c.sibling; ) {
            if (null === c.return || c.return === b)
              return;
            c = c.return;
          }
          c.sibling.return = c.return;
          c = c.sibling;
        }
      };
      Aj = function() {
      };
      Bj = function(a, b, c, d) {
        var e = a.memoizedProps;
        if (e !== d) {
          a = b.stateNode;
          xh(uh.current);
          var f = null;
          switch (c) {
            case "input":
              e = Ya(a, e);
              d = Ya(a, d);
              f = [];
              break;
            case "select":
              e = A({}, e, { value: void 0 });
              d = A({}, d, { value: void 0 });
              f = [];
              break;
            case "textarea":
              e = gb(a, e);
              d = gb(a, d);
              f = [];
              break;
            default:
              "function" !== typeof e.onClick && "function" === typeof d.onClick && (a.onclick = Bf);
          }
          ub(c, d);
          var g;
          c = null;
          for (l in e)
            if (!d.hasOwnProperty(l) && e.hasOwnProperty(l) && null != e[l])
              if ("style" === l) {
                var h = e[l];
                for (g in h)
                  h.hasOwnProperty(g) && (c || (c = {}), c[g] = "");
              } else
                "dangerouslySetInnerHTML" !== l && "children" !== l && "suppressContentEditableWarning" !== l && "suppressHydrationWarning" !== l && "autoFocus" !== l && (ea.hasOwnProperty(l) ? f || (f = []) : (f = f || []).push(l, null));
          for (l in d) {
            var k = d[l];
            h = null != e ? e[l] : void 0;
            if (d.hasOwnProperty(l) && k !== h && (null != k || null != h))
              if ("style" === l)
                if (h) {
                  for (g in h)
                    !h.hasOwnProperty(g) || k && k.hasOwnProperty(g) || (c || (c = {}), c[g] = "");
                  for (g in k)
                    k.hasOwnProperty(g) && h[g] !== k[g] && (c || (c = {}), c[g] = k[g]);
                } else
                  c || (f || (f = []), f.push(
                    l,
                    c
                  )), c = k;
              else
                "dangerouslySetInnerHTML" === l ? (k = k ? k.__html : void 0, h = h ? h.__html : void 0, null != k && h !== k && (f = f || []).push(l, k)) : "children" === l ? "string" !== typeof k && "number" !== typeof k || (f = f || []).push(l, "" + k) : "suppressContentEditableWarning" !== l && "suppressHydrationWarning" !== l && (ea.hasOwnProperty(l) ? (null != k && "onScroll" === l && D("scroll", a), f || h === k || (f = [])) : (f = f || []).push(l, k));
          }
          c && (f = f || []).push("style", c);
          var l = f;
          if (b.updateQueue = l)
            b.flags |= 4;
        }
      };
      Cj = function(a, b, c, d) {
        c !== d && (b.flags |= 4);
      };
      function Dj(a, b) {
        if (!I)
          switch (a.tailMode) {
            case "hidden":
              b = a.tail;
              for (var c = null; null !== b; )
                null !== b.alternate && (c = b), b = b.sibling;
              null === c ? a.tail = null : c.sibling = null;
              break;
            case "collapsed":
              c = a.tail;
              for (var d = null; null !== c; )
                null !== c.alternate && (d = c), c = c.sibling;
              null === d ? b || null === a.tail ? a.tail = null : a.tail.sibling = null : d.sibling = null;
          }
      }
      function S2(a) {
        var b = null !== a.alternate && a.alternate.child === a.child, c = 0, d = 0;
        if (b)
          for (var e = a.child; null !== e; )
            c |= e.lanes | e.childLanes, d |= e.subtreeFlags & 14680064, d |= e.flags & 14680064, e.return = a, e = e.sibling;
        else
          for (e = a.child; null !== e; )
            c |= e.lanes | e.childLanes, d |= e.subtreeFlags, d |= e.flags, e.return = a, e = e.sibling;
        a.subtreeFlags |= d;
        a.childLanes = c;
        return b;
      }
      function Ej(a, b, c) {
        var d = b.pendingProps;
        wg(b);
        switch (b.tag) {
          case 2:
          case 16:
          case 15:
          case 0:
          case 11:
          case 7:
          case 8:
          case 12:
          case 9:
          case 14:
            return S2(b), null;
          case 1:
            return Zf(b.type) && $f(), S2(b), null;
          case 3:
            d = b.stateNode;
            zh();
            E(Wf);
            E(H12);
            Eh();
            d.pendingContext && (d.context = d.pendingContext, d.pendingContext = null);
            if (null === a || null === a.child)
              Gg(b) ? b.flags |= 4 : null === a || a.memoizedState.isDehydrated && 0 === (b.flags & 256) || (b.flags |= 1024, null !== zg && (Fj(zg), zg = null));
            Aj(a, b);
            S2(b);
            return null;
          case 5:
            Bh(b);
            var e = xh(wh.current);
            c = b.type;
            if (null !== a && null != b.stateNode)
              Bj(a, b, c, d, e), a.ref !== b.ref && (b.flags |= 512, b.flags |= 2097152);
            else {
              if (!d) {
                if (null === b.stateNode)
                  throw Error(p(166));
                S2(b);
                return null;
              }
              a = xh(uh.current);
              if (Gg(b)) {
                d = b.stateNode;
                c = b.type;
                var f = b.memoizedProps;
                d[Of] = b;
                d[Pf] = f;
                a = 0 !== (b.mode & 1);
                switch (c) {
                  case "dialog":
                    D("cancel", d);
                    D("close", d);
                    break;
                  case "iframe":
                  case "object":
                  case "embed":
                    D("load", d);
                    break;
                  case "video":
                  case "audio":
                    for (e = 0; e < lf.length; e++)
                      D(lf[e], d);
                    break;
                  case "source":
                    D("error", d);
                    break;
                  case "img":
                  case "image":
                  case "link":
                    D(
                      "error",
                      d
                    );
                    D("load", d);
                    break;
                  case "details":
                    D("toggle", d);
                    break;
                  case "input":
                    Za(d, f);
                    D("invalid", d);
                    break;
                  case "select":
                    d._wrapperState = { wasMultiple: !!f.multiple };
                    D("invalid", d);
                    break;
                  case "textarea":
                    hb(d, f), D("invalid", d);
                }
                ub(c, f);
                e = null;
                for (var g in f)
                  if (f.hasOwnProperty(g)) {
                    var h = f[g];
                    "children" === g ? "string" === typeof h ? d.textContent !== h && (true !== f.suppressHydrationWarning && Af(d.textContent, h, a), e = ["children", h]) : "number" === typeof h && d.textContent !== "" + h && (true !== f.suppressHydrationWarning && Af(
                      d.textContent,
                      h,
                      a
                    ), e = ["children", "" + h]) : ea.hasOwnProperty(g) && null != h && "onScroll" === g && D("scroll", d);
                  }
                switch (c) {
                  case "input":
                    Va(d);
                    db(d, f, true);
                    break;
                  case "textarea":
                    Va(d);
                    jb(d);
                    break;
                  case "select":
                  case "option":
                    break;
                  default:
                    "function" === typeof f.onClick && (d.onclick = Bf);
                }
                d = e;
                b.updateQueue = d;
                null !== d && (b.flags |= 4);
              } else {
                g = 9 === e.nodeType ? e : e.ownerDocument;
                "http://www.w3.org/1999/xhtml" === a && (a = kb(c));
                "http://www.w3.org/1999/xhtml" === a ? "script" === c ? (a = g.createElement("div"), a.innerHTML = "<script><\/script>", a = a.removeChild(a.firstChild)) : "string" === typeof d.is ? a = g.createElement(c, { is: d.is }) : (a = g.createElement(c), "select" === c && (g = a, d.multiple ? g.multiple = true : d.size && (g.size = d.size))) : a = g.createElementNS(a, c);
                a[Of] = b;
                a[Pf] = d;
                zj(a, b, false, false);
                b.stateNode = a;
                a: {
                  g = vb(c, d);
                  switch (c) {
                    case "dialog":
                      D("cancel", a);
                      D("close", a);
                      e = d;
                      break;
                    case "iframe":
                    case "object":
                    case "embed":
                      D("load", a);
                      e = d;
                      break;
                    case "video":
                    case "audio":
                      for (e = 0; e < lf.length; e++)
                        D(lf[e], a);
                      e = d;
                      break;
                    case "source":
                      D("error", a);
                      e = d;
                      break;
                    case "img":
                    case "image":
                    case "link":
                      D(
                        "error",
                        a
                      );
                      D("load", a);
                      e = d;
                      break;
                    case "details":
                      D("toggle", a);
                      e = d;
                      break;
                    case "input":
                      Za(a, d);
                      e = Ya(a, d);
                      D("invalid", a);
                      break;
                    case "option":
                      e = d;
                      break;
                    case "select":
                      a._wrapperState = { wasMultiple: !!d.multiple };
                      e = A({}, d, { value: void 0 });
                      D("invalid", a);
                      break;
                    case "textarea":
                      hb(a, d);
                      e = gb(a, d);
                      D("invalid", a);
                      break;
                    default:
                      e = d;
                  }
                  ub(c, e);
                  h = e;
                  for (f in h)
                    if (h.hasOwnProperty(f)) {
                      var k = h[f];
                      "style" === f ? sb(a, k) : "dangerouslySetInnerHTML" === f ? (k = k ? k.__html : void 0, null != k && nb(a, k)) : "children" === f ? "string" === typeof k ? ("textarea" !== c || "" !== k) && ob(a, k) : "number" === typeof k && ob(a, "" + k) : "suppressContentEditableWarning" !== f && "suppressHydrationWarning" !== f && "autoFocus" !== f && (ea.hasOwnProperty(f) ? null != k && "onScroll" === f && D("scroll", a) : null != k && ta(a, f, k, g));
                    }
                  switch (c) {
                    case "input":
                      Va(a);
                      db(a, d, false);
                      break;
                    case "textarea":
                      Va(a);
                      jb(a);
                      break;
                    case "option":
                      null != d.value && a.setAttribute("value", "" + Sa(d.value));
                      break;
                    case "select":
                      a.multiple = !!d.multiple;
                      f = d.value;
                      null != f ? fb(a, !!d.multiple, f, false) : null != d.defaultValue && fb(
                        a,
                        !!d.multiple,
                        d.defaultValue,
                        true
                      );
                      break;
                    default:
                      "function" === typeof e.onClick && (a.onclick = Bf);
                  }
                  switch (c) {
                    case "button":
                    case "input":
                    case "select":
                    case "textarea":
                      d = !!d.autoFocus;
                      break a;
                    case "img":
                      d = true;
                      break a;
                    default:
                      d = false;
                  }
                }
                d && (b.flags |= 4);
              }
              null !== b.ref && (b.flags |= 512, b.flags |= 2097152);
            }
            S2(b);
            return null;
          case 6:
            if (a && null != b.stateNode)
              Cj(a, b, a.memoizedProps, d);
            else {
              if ("string" !== typeof d && null === b.stateNode)
                throw Error(p(166));
              c = xh(wh.current);
              xh(uh.current);
              if (Gg(b)) {
                d = b.stateNode;
                c = b.memoizedProps;
                d[Of] = b;
                if (f = d.nodeValue !== c) {
                  if (a = xg, null !== a)
                    switch (a.tag) {
                      case 3:
                        Af(d.nodeValue, c, 0 !== (a.mode & 1));
                        break;
                      case 5:
                        true !== a.memoizedProps.suppressHydrationWarning && Af(d.nodeValue, c, 0 !== (a.mode & 1));
                    }
                }
                f && (b.flags |= 4);
              } else
                d = (9 === c.nodeType ? c : c.ownerDocument).createTextNode(d), d[Of] = b, b.stateNode = d;
            }
            S2(b);
            return null;
          case 13:
            E(L);
            d = b.memoizedState;
            if (null === a || null !== a.memoizedState && null !== a.memoizedState.dehydrated) {
              if (I && null !== yg && 0 !== (b.mode & 1) && 0 === (b.flags & 128))
                Hg(), Ig(), b.flags |= 98560, f = false;
              else if (f = Gg(b), null !== d && null !== d.dehydrated) {
                if (null === a) {
                  if (!f)
                    throw Error(p(318));
                  f = b.memoizedState;
                  f = null !== f ? f.dehydrated : null;
                  if (!f)
                    throw Error(p(317));
                  f[Of] = b;
                } else
                  Ig(), 0 === (b.flags & 128) && (b.memoizedState = null), b.flags |= 4;
                S2(b);
                f = false;
              } else
                null !== zg && (Fj(zg), zg = null), f = true;
              if (!f)
                return b.flags & 65536 ? b : null;
            }
            if (0 !== (b.flags & 128))
              return b.lanes = c, b;
            d = null !== d;
            d !== (null !== a && null !== a.memoizedState) && d && (b.child.flags |= 8192, 0 !== (b.mode & 1) && (null === a || 0 !== (L.current & 1) ? 0 === T2 && (T2 = 3) : tj()));
            null !== b.updateQueue && (b.flags |= 4);
            S2(b);
            return null;
          case 4:
            return zh(), Aj(a, b), null === a && sf(b.stateNode.containerInfo), S2(b), null;
          case 10:
            return ah(b.type._context), S2(b), null;
          case 17:
            return Zf(b.type) && $f(), S2(b), null;
          case 19:
            E(L);
            f = b.memoizedState;
            if (null === f)
              return S2(b), null;
            d = 0 !== (b.flags & 128);
            g = f.rendering;
            if (null === g)
              if (d)
                Dj(f, false);
              else {
                if (0 !== T2 || null !== a && 0 !== (a.flags & 128))
                  for (a = b.child; null !== a; ) {
                    g = Ch(a);
                    if (null !== g) {
                      b.flags |= 128;
                      Dj(f, false);
                      d = g.updateQueue;
                      null !== d && (b.updateQueue = d, b.flags |= 4);
                      b.subtreeFlags = 0;
                      d = c;
                      for (c = b.child; null !== c; )
                        f = c, a = d, f.flags &= 14680066, g = f.alternate, null === g ? (f.childLanes = 0, f.lanes = a, f.child = null, f.subtreeFlags = 0, f.memoizedProps = null, f.memoizedState = null, f.updateQueue = null, f.dependencies = null, f.stateNode = null) : (f.childLanes = g.childLanes, f.lanes = g.lanes, f.child = g.child, f.subtreeFlags = 0, f.deletions = null, f.memoizedProps = g.memoizedProps, f.memoizedState = g.memoizedState, f.updateQueue = g.updateQueue, f.type = g.type, a = g.dependencies, f.dependencies = null === a ? null : { lanes: a.lanes, firstContext: a.firstContext }), c = c.sibling;
                      G4(L, L.current & 1 | 2);
                      return b.child;
                    }
                    a = a.sibling;
                  }
                null !== f.tail && B() > Gj && (b.flags |= 128, d = true, Dj(f, false), b.lanes = 4194304);
              }
            else {
              if (!d)
                if (a = Ch(g), null !== a) {
                  if (b.flags |= 128, d = true, c = a.updateQueue, null !== c && (b.updateQueue = c, b.flags |= 4), Dj(f, true), null === f.tail && "hidden" === f.tailMode && !g.alternate && !I)
                    return S2(b), null;
                } else
                  2 * B() - f.renderingStartTime > Gj && 1073741824 !== c && (b.flags |= 128, d = true, Dj(f, false), b.lanes = 4194304);
              f.isBackwards ? (g.sibling = b.child, b.child = g) : (c = f.last, null !== c ? c.sibling = g : b.child = g, f.last = g);
            }
            if (null !== f.tail)
              return b = f.tail, f.rendering = b, f.tail = b.sibling, f.renderingStartTime = B(), b.sibling = null, c = L.current, G4(L, d ? c & 1 | 2 : c & 1), b;
            S2(b);
            return null;
          case 22:
          case 23:
            return Hj(), d = null !== b.memoizedState, null !== a && null !== a.memoizedState !== d && (b.flags |= 8192), d && 0 !== (b.mode & 1) ? 0 !== (fj & 1073741824) && (S2(b), b.subtreeFlags & 6 && (b.flags |= 8192)) : S2(b), null;
          case 24:
            return null;
          case 25:
            return null;
        }
        throw Error(p(156, b.tag));
      }
      function Ij(a, b) {
        wg(b);
        switch (b.tag) {
          case 1:
            return Zf(b.type) && $f(), a = b.flags, a & 65536 ? (b.flags = a & -65537 | 128, b) : null;
          case 3:
            return zh(), E(Wf), E(H12), Eh(), a = b.flags, 0 !== (a & 65536) && 0 === (a & 128) ? (b.flags = a & -65537 | 128, b) : null;
          case 5:
            return Bh(b), null;
          case 13:
            E(L);
            a = b.memoizedState;
            if (null !== a && null !== a.dehydrated) {
              if (null === b.alternate)
                throw Error(p(340));
              Ig();
            }
            a = b.flags;
            return a & 65536 ? (b.flags = a & -65537 | 128, b) : null;
          case 19:
            return E(L), null;
          case 4:
            return zh(), null;
          case 10:
            return ah(b.type._context), null;
          case 22:
          case 23:
            return Hj(), null;
          case 24:
            return null;
          default:
            return null;
        }
      }
      var Jj = false;
      var U = false;
      var Kj = "function" === typeof WeakSet ? WeakSet : Set;
      var V = null;
      function Lj(a, b) {
        var c = a.ref;
        if (null !== c)
          if ("function" === typeof c)
            try {
              c(null);
            } catch (d) {
              W12(a, b, d);
            }
          else
            c.current = null;
      }
      function Mj(a, b, c) {
        try {
          c();
        } catch (d) {
          W12(a, b, d);
        }
      }
      var Nj = false;
      function Oj(a, b) {
        Cf = dd;
        a = Me();
        if (Ne(a)) {
          if ("selectionStart" in a)
            var c = { start: a.selectionStart, end: a.selectionEnd };
          else
            a: {
              c = (c = a.ownerDocument) && c.defaultView || window;
              var d = c.getSelection && c.getSelection();
              if (d && 0 !== d.rangeCount) {
                c = d.anchorNode;
                var e = d.anchorOffset, f = d.focusNode;
                d = d.focusOffset;
                try {
                  c.nodeType, f.nodeType;
                } catch (F) {
                  c = null;
                  break a;
                }
                var g = 0, h = -1, k = -1, l = 0, m = 0, q = a, r = null;
                b:
                  for (; ; ) {
                    for (var y; ; ) {
                      q !== c || 0 !== e && 3 !== q.nodeType || (h = g + e);
                      q !== f || 0 !== d && 3 !== q.nodeType || (k = g + d);
                      3 === q.nodeType && (g += q.nodeValue.length);
                      if (null === (y = q.firstChild))
                        break;
                      r = q;
                      q = y;
                    }
                    for (; ; ) {
                      if (q === a)
                        break b;
                      r === c && ++l === e && (h = g);
                      r === f && ++m === d && (k = g);
                      if (null !== (y = q.nextSibling))
                        break;
                      q = r;
                      r = q.parentNode;
                    }
                    q = y;
                  }
                c = -1 === h || -1 === k ? null : { start: h, end: k };
              } else
                c = null;
            }
          c = c || { start: 0, end: 0 };
        } else
          c = null;
        Df = { focusedElem: a, selectionRange: c };
        dd = false;
        for (V = b; null !== V; )
          if (b = V, a = b.child, 0 !== (b.subtreeFlags & 1028) && null !== a)
            a.return = b, V = a;
          else
            for (; null !== V; ) {
              b = V;
              try {
                var n = b.alternate;
                if (0 !== (b.flags & 1024))
                  switch (b.tag) {
                    case 0:
                    case 11:
                    case 15:
                      break;
                    case 1:
                      if (null !== n) {
                        var t = n.memoizedProps, J = n.memoizedState, x = b.stateNode, w = x.getSnapshotBeforeUpdate(b.elementType === b.type ? t : Ci(b.type, t), J);
                        x.__reactInternalSnapshotBeforeUpdate = w;
                      }
                      break;
                    case 3:
                      var u = b.stateNode.containerInfo;
                      1 === u.nodeType ? u.textContent = "" : 9 === u.nodeType && u.documentElement && u.removeChild(u.documentElement);
                      break;
                    case 5:
                    case 6:
                    case 4:
                    case 17:
                      break;
                    default:
                      throw Error(p(163));
                  }
              } catch (F) {
                W12(b, b.return, F);
              }
              a = b.sibling;
              if (null !== a) {
                a.return = b.return;
                V = a;
                break;
              }
              V = b.return;
            }
        n = Nj;
        Nj = false;
        return n;
      }
      function Pj(a, b, c) {
        var d = b.updateQueue;
        d = null !== d ? d.lastEffect : null;
        if (null !== d) {
          var e = d = d.next;
          do {
            if ((e.tag & a) === a) {
              var f = e.destroy;
              e.destroy = void 0;
              void 0 !== f && Mj(b, c, f);
            }
            e = e.next;
          } while (e !== d);
        }
      }
      function Qj(a, b) {
        b = b.updateQueue;
        b = null !== b ? b.lastEffect : null;
        if (null !== b) {
          var c = b = b.next;
          do {
            if ((c.tag & a) === a) {
              var d = c.create;
              c.destroy = d();
            }
            c = c.next;
          } while (c !== b);
        }
      }
      function Rj(a) {
        var b = a.ref;
        if (null !== b) {
          var c = a.stateNode;
          switch (a.tag) {
            case 5:
              a = c;
              break;
            default:
              a = c;
          }
          "function" === typeof b ? b(a) : b.current = a;
        }
      }
      function Sj(a) {
        var b = a.alternate;
        null !== b && (a.alternate = null, Sj(b));
        a.child = null;
        a.deletions = null;
        a.sibling = null;
        5 === a.tag && (b = a.stateNode, null !== b && (delete b[Of], delete b[Pf], delete b[of], delete b[Qf], delete b[Rf]));
        a.stateNode = null;
        a.return = null;
        a.dependencies = null;
        a.memoizedProps = null;
        a.memoizedState = null;
        a.pendingProps = null;
        a.stateNode = null;
        a.updateQueue = null;
      }
      function Tj(a) {
        return 5 === a.tag || 3 === a.tag || 4 === a.tag;
      }
      function Uj(a) {
        a:
          for (; ; ) {
            for (; null === a.sibling; ) {
              if (null === a.return || Tj(a.return))
                return null;
              a = a.return;
            }
            a.sibling.return = a.return;
            for (a = a.sibling; 5 !== a.tag && 6 !== a.tag && 18 !== a.tag; ) {
              if (a.flags & 2)
                continue a;
              if (null === a.child || 4 === a.tag)
                continue a;
              else
                a.child.return = a, a = a.child;
            }
            if (!(a.flags & 2))
              return a.stateNode;
          }
      }
      function Vj(a, b, c) {
        var d = a.tag;
        if (5 === d || 6 === d)
          a = a.stateNode, b ? 8 === c.nodeType ? c.parentNode.insertBefore(a, b) : c.insertBefore(a, b) : (8 === c.nodeType ? (b = c.parentNode, b.insertBefore(a, c)) : (b = c, b.appendChild(a)), c = c._reactRootContainer, null !== c && void 0 !== c || null !== b.onclick || (b.onclick = Bf));
        else if (4 !== d && (a = a.child, null !== a))
          for (Vj(a, b, c), a = a.sibling; null !== a; )
            Vj(a, b, c), a = a.sibling;
      }
      function Wj(a, b, c) {
        var d = a.tag;
        if (5 === d || 6 === d)
          a = a.stateNode, b ? c.insertBefore(a, b) : c.appendChild(a);
        else if (4 !== d && (a = a.child, null !== a))
          for (Wj(a, b, c), a = a.sibling; null !== a; )
            Wj(a, b, c), a = a.sibling;
      }
      var X = null;
      var Xj = false;
      function Yj(a, b, c) {
        for (c = c.child; null !== c; )
          Zj(a, b, c), c = c.sibling;
      }
      function Zj(a, b, c) {
        if (lc && "function" === typeof lc.onCommitFiberUnmount)
          try {
            lc.onCommitFiberUnmount(kc, c);
          } catch (h) {
          }
        switch (c.tag) {
          case 5:
            U || Lj(c, b);
          case 6:
            var d = X, e = Xj;
            X = null;
            Yj(a, b, c);
            X = d;
            Xj = e;
            null !== X && (Xj ? (a = X, c = c.stateNode, 8 === a.nodeType ? a.parentNode.removeChild(c) : a.removeChild(c)) : X.removeChild(c.stateNode));
            break;
          case 18:
            null !== X && (Xj ? (a = X, c = c.stateNode, 8 === a.nodeType ? Kf(a.parentNode, c) : 1 === a.nodeType && Kf(a, c), bd(a)) : Kf(X, c.stateNode));
            break;
          case 4:
            d = X;
            e = Xj;
            X = c.stateNode.containerInfo;
            Xj = true;
            Yj(a, b, c);
            X = d;
            Xj = e;
            break;
          case 0:
          case 11:
          case 14:
          case 15:
            if (!U && (d = c.updateQueue, null !== d && (d = d.lastEffect, null !== d))) {
              e = d = d.next;
              do {
                var f = e, g = f.destroy;
                f = f.tag;
                void 0 !== g && (0 !== (f & 2) ? Mj(c, b, g) : 0 !== (f & 4) && Mj(c, b, g));
                e = e.next;
              } while (e !== d);
            }
            Yj(a, b, c);
            break;
          case 1:
            if (!U && (Lj(c, b), d = c.stateNode, "function" === typeof d.componentWillUnmount))
              try {
                d.props = c.memoizedProps, d.state = c.memoizedState, d.componentWillUnmount();
              } catch (h) {
                W12(c, b, h);
              }
            Yj(a, b, c);
            break;
          case 21:
            Yj(a, b, c);
            break;
          case 22:
            c.mode & 1 ? (U = (d = U) || null !== c.memoizedState, Yj(a, b, c), U = d) : Yj(a, b, c);
            break;
          default:
            Yj(a, b, c);
        }
      }
      function ak(a) {
        var b = a.updateQueue;
        if (null !== b) {
          a.updateQueue = null;
          var c = a.stateNode;
          null === c && (c = a.stateNode = new Kj());
          b.forEach(function(b2) {
            var d = bk.bind(null, a, b2);
            c.has(b2) || (c.add(b2), b2.then(d, d));
          });
        }
      }
      function ck(a, b) {
        var c = b.deletions;
        if (null !== c)
          for (var d = 0; d < c.length; d++) {
            var e = c[d];
            try {
              var f = a, g = b, h = g;
              a:
                for (; null !== h; ) {
                  switch (h.tag) {
                    case 5:
                      X = h.stateNode;
                      Xj = false;
                      break a;
                    case 3:
                      X = h.stateNode.containerInfo;
                      Xj = true;
                      break a;
                    case 4:
                      X = h.stateNode.containerInfo;
                      Xj = true;
                      break a;
                  }
                  h = h.return;
                }
              if (null === X)
                throw Error(p(160));
              Zj(f, g, e);
              X = null;
              Xj = false;
              var k = e.alternate;
              null !== k && (k.return = null);
              e.return = null;
            } catch (l) {
              W12(e, b, l);
            }
          }
        if (b.subtreeFlags & 12854)
          for (b = b.child; null !== b; )
            dk(b, a), b = b.sibling;
      }
      function dk(a, b) {
        var c = a.alternate, d = a.flags;
        switch (a.tag) {
          case 0:
          case 11:
          case 14:
          case 15:
            ck(b, a);
            ek(a);
            if (d & 4) {
              try {
                Pj(3, a, a.return), Qj(3, a);
              } catch (t) {
                W12(a, a.return, t);
              }
              try {
                Pj(5, a, a.return);
              } catch (t) {
                W12(a, a.return, t);
              }
            }
            break;
          case 1:
            ck(b, a);
            ek(a);
            d & 512 && null !== c && Lj(c, c.return);
            break;
          case 5:
            ck(b, a);
            ek(a);
            d & 512 && null !== c && Lj(c, c.return);
            if (a.flags & 32) {
              var e = a.stateNode;
              try {
                ob(e, "");
              } catch (t) {
                W12(a, a.return, t);
              }
            }
            if (d & 4 && (e = a.stateNode, null != e)) {
              var f = a.memoizedProps, g = null !== c ? c.memoizedProps : f, h = a.type, k = a.updateQueue;
              a.updateQueue = null;
              if (null !== k)
                try {
                  "input" === h && "radio" === f.type && null != f.name && ab(e, f);
                  vb(h, g);
                  var l = vb(h, f);
                  for (g = 0; g < k.length; g += 2) {
                    var m = k[g], q = k[g + 1];
                    "style" === m ? sb(e, q) : "dangerouslySetInnerHTML" === m ? nb(e, q) : "children" === m ? ob(e, q) : ta(e, m, q, l);
                  }
                  switch (h) {
                    case "input":
                      bb(e, f);
                      break;
                    case "textarea":
                      ib(e, f);
                      break;
                    case "select":
                      var r = e._wrapperState.wasMultiple;
                      e._wrapperState.wasMultiple = !!f.multiple;
                      var y = f.value;
                      null != y ? fb(e, !!f.multiple, y, false) : r !== !!f.multiple && (null != f.defaultValue ? fb(
                        e,
                        !!f.multiple,
                        f.defaultValue,
                        true
                      ) : fb(e, !!f.multiple, f.multiple ? [] : "", false));
                  }
                  e[Pf] = f;
                } catch (t) {
                  W12(a, a.return, t);
                }
            }
            break;
          case 6:
            ck(b, a);
            ek(a);
            if (d & 4) {
              if (null === a.stateNode)
                throw Error(p(162));
              e = a.stateNode;
              f = a.memoizedProps;
              try {
                e.nodeValue = f;
              } catch (t) {
                W12(a, a.return, t);
              }
            }
            break;
          case 3:
            ck(b, a);
            ek(a);
            if (d & 4 && null !== c && c.memoizedState.isDehydrated)
              try {
                bd(b.containerInfo);
              } catch (t) {
                W12(a, a.return, t);
              }
            break;
          case 4:
            ck(b, a);
            ek(a);
            break;
          case 13:
            ck(b, a);
            ek(a);
            e = a.child;
            e.flags & 8192 && (f = null !== e.memoizedState, e.stateNode.isHidden = f, !f || null !== e.alternate && null !== e.alternate.memoizedState || (fk = B()));
            d & 4 && ak(a);
            break;
          case 22:
            m = null !== c && null !== c.memoizedState;
            a.mode & 1 ? (U = (l = U) || m, ck(b, a), U = l) : ck(b, a);
            ek(a);
            if (d & 8192) {
              l = null !== a.memoizedState;
              if ((a.stateNode.isHidden = l) && !m && 0 !== (a.mode & 1))
                for (V = a, m = a.child; null !== m; ) {
                  for (q = V = m; null !== V; ) {
                    r = V;
                    y = r.child;
                    switch (r.tag) {
                      case 0:
                      case 11:
                      case 14:
                      case 15:
                        Pj(4, r, r.return);
                        break;
                      case 1:
                        Lj(r, r.return);
                        var n = r.stateNode;
                        if ("function" === typeof n.componentWillUnmount) {
                          d = r;
                          c = r.return;
                          try {
                            b = d, n.props = b.memoizedProps, n.state = b.memoizedState, n.componentWillUnmount();
                          } catch (t) {
                            W12(d, c, t);
                          }
                        }
                        break;
                      case 5:
                        Lj(r, r.return);
                        break;
                      case 22:
                        if (null !== r.memoizedState) {
                          gk(q);
                          continue;
                        }
                    }
                    null !== y ? (y.return = r, V = y) : gk(q);
                  }
                  m = m.sibling;
                }
              a:
                for (m = null, q = a; ; ) {
                  if (5 === q.tag) {
                    if (null === m) {
                      m = q;
                      try {
                        e = q.stateNode, l ? (f = e.style, "function" === typeof f.setProperty ? f.setProperty("display", "none", "important") : f.display = "none") : (h = q.stateNode, k = q.memoizedProps.style, g = void 0 !== k && null !== k && k.hasOwnProperty("display") ? k.display : null, h.style.display = rb("display", g));
                      } catch (t) {
                        W12(a, a.return, t);
                      }
                    }
                  } else if (6 === q.tag) {
                    if (null === m)
                      try {
                        q.stateNode.nodeValue = l ? "" : q.memoizedProps;
                      } catch (t) {
                        W12(a, a.return, t);
                      }
                  } else if ((22 !== q.tag && 23 !== q.tag || null === q.memoizedState || q === a) && null !== q.child) {
                    q.child.return = q;
                    q = q.child;
                    continue;
                  }
                  if (q === a)
                    break a;
                  for (; null === q.sibling; ) {
                    if (null === q.return || q.return === a)
                      break a;
                    m === q && (m = null);
                    q = q.return;
                  }
                  m === q && (m = null);
                  q.sibling.return = q.return;
                  q = q.sibling;
                }
            }
            break;
          case 19:
            ck(b, a);
            ek(a);
            d & 4 && ak(a);
            break;
          case 21:
            break;
          default:
            ck(
              b,
              a
            ), ek(a);
        }
      }
      function ek(a) {
        var b = a.flags;
        if (b & 2) {
          try {
            a: {
              for (var c = a.return; null !== c; ) {
                if (Tj(c)) {
                  var d = c;
                  break a;
                }
                c = c.return;
              }
              throw Error(p(160));
            }
            switch (d.tag) {
              case 5:
                var e = d.stateNode;
                d.flags & 32 && (ob(e, ""), d.flags &= -33);
                var f = Uj(a);
                Wj(a, f, e);
                break;
              case 3:
              case 4:
                var g = d.stateNode.containerInfo, h = Uj(a);
                Vj(a, h, g);
                break;
              default:
                throw Error(p(161));
            }
          } catch (k) {
            W12(a, a.return, k);
          }
          a.flags &= -3;
        }
        b & 4096 && (a.flags &= -4097);
      }
      function hk(a, b, c) {
        V = a;
        ik(a, b, c);
      }
      function ik(a, b, c) {
        for (var d = 0 !== (a.mode & 1); null !== V; ) {
          var e = V, f = e.child;
          if (22 === e.tag && d) {
            var g = null !== e.memoizedState || Jj;
            if (!g) {
              var h = e.alternate, k = null !== h && null !== h.memoizedState || U;
              h = Jj;
              var l = U;
              Jj = g;
              if ((U = k) && !l)
                for (V = e; null !== V; )
                  g = V, k = g.child, 22 === g.tag && null !== g.memoizedState ? jk(e) : null !== k ? (k.return = g, V = k) : jk(e);
              for (; null !== f; )
                V = f, ik(f, b, c), f = f.sibling;
              V = e;
              Jj = h;
              U = l;
            }
            kk(a, b, c);
          } else
            0 !== (e.subtreeFlags & 8772) && null !== f ? (f.return = e, V = f) : kk(a, b, c);
        }
      }
      function kk(a) {
        for (; null !== V; ) {
          var b = V;
          if (0 !== (b.flags & 8772)) {
            var c = b.alternate;
            try {
              if (0 !== (b.flags & 8772))
                switch (b.tag) {
                  case 0:
                  case 11:
                  case 15:
                    U || Qj(5, b);
                    break;
                  case 1:
                    var d = b.stateNode;
                    if (b.flags & 4 && !U)
                      if (null === c)
                        d.componentDidMount();
                      else {
                        var e = b.elementType === b.type ? c.memoizedProps : Ci(b.type, c.memoizedProps);
                        d.componentDidUpdate(e, c.memoizedState, d.__reactInternalSnapshotBeforeUpdate);
                      }
                    var f = b.updateQueue;
                    null !== f && sh(b, f, d);
                    break;
                  case 3:
                    var g = b.updateQueue;
                    if (null !== g) {
                      c = null;
                      if (null !== b.child)
                        switch (b.child.tag) {
                          case 5:
                            c = b.child.stateNode;
                            break;
                          case 1:
                            c = b.child.stateNode;
                        }
                      sh(b, g, c);
                    }
                    break;
                  case 5:
                    var h = b.stateNode;
                    if (null === c && b.flags & 4) {
                      c = h;
                      var k = b.memoizedProps;
                      switch (b.type) {
                        case "button":
                        case "input":
                        case "select":
                        case "textarea":
                          k.autoFocus && c.focus();
                          break;
                        case "img":
                          k.src && (c.src = k.src);
                      }
                    }
                    break;
                  case 6:
                    break;
                  case 4:
                    break;
                  case 12:
                    break;
                  case 13:
                    if (null === b.memoizedState) {
                      var l = b.alternate;
                      if (null !== l) {
                        var m = l.memoizedState;
                        if (null !== m) {
                          var q = m.dehydrated;
                          null !== q && bd(q);
                        }
                      }
                    }
                    break;
                  case 19:
                  case 17:
                  case 21:
                  case 22:
                  case 23:
                  case 25:
                    break;
                  default:
                    throw Error(p(163));
                }
              U || b.flags & 512 && Rj(b);
            } catch (r) {
              W12(b, b.return, r);
            }
          }
          if (b === a) {
            V = null;
            break;
          }
          c = b.sibling;
          if (null !== c) {
            c.return = b.return;
            V = c;
            break;
          }
          V = b.return;
        }
      }
      function gk(a) {
        for (; null !== V; ) {
          var b = V;
          if (b === a) {
            V = null;
            break;
          }
          var c = b.sibling;
          if (null !== c) {
            c.return = b.return;
            V = c;
            break;
          }
          V = b.return;
        }
      }
      function jk(a) {
        for (; null !== V; ) {
          var b = V;
          try {
            switch (b.tag) {
              case 0:
              case 11:
              case 15:
                var c = b.return;
                try {
                  Qj(4, b);
                } catch (k) {
                  W12(b, c, k);
                }
                break;
              case 1:
                var d = b.stateNode;
                if ("function" === typeof d.componentDidMount) {
                  var e = b.return;
                  try {
                    d.componentDidMount();
                  } catch (k) {
                    W12(b, e, k);
                  }
                }
                var f = b.return;
                try {
                  Rj(b);
                } catch (k) {
                  W12(b, f, k);
                }
                break;
              case 5:
                var g = b.return;
                try {
                  Rj(b);
                } catch (k) {
                  W12(b, g, k);
                }
            }
          } catch (k) {
            W12(b, b.return, k);
          }
          if (b === a) {
            V = null;
            break;
          }
          var h = b.sibling;
          if (null !== h) {
            h.return = b.return;
            V = h;
            break;
          }
          V = b.return;
        }
      }
      var lk = Math.ceil;
      var mk = ua.ReactCurrentDispatcher;
      var nk = ua.ReactCurrentOwner;
      var ok = ua.ReactCurrentBatchConfig;
      var K = 0;
      var Q = null;
      var Y = null;
      var Z = 0;
      var fj = 0;
      var ej = Uf(0);
      var T2 = 0;
      var pk = null;
      var rh = 0;
      var qk = 0;
      var rk = 0;
      var sk = null;
      var tk = null;
      var fk = 0;
      var Gj = Infinity;
      var uk = null;
      var Oi = false;
      var Pi = null;
      var Ri = null;
      var vk = false;
      var wk = null;
      var xk = 0;
      var yk = 0;
      var zk = null;
      var Ak = -1;
      var Bk = 0;
      function R() {
        return 0 !== (K & 6) ? B() : -1 !== Ak ? Ak : Ak = B();
      }
      function yi(a) {
        if (0 === (a.mode & 1))
          return 1;
        if (0 !== (K & 2) && 0 !== Z)
          return Z & -Z;
        if (null !== Kg.transition)
          return 0 === Bk && (Bk = yc()), Bk;
        a = C3;
        if (0 !== a)
          return a;
        a = window.event;
        a = void 0 === a ? 16 : jd(a.type);
        return a;
      }
      function gi(a, b, c, d) {
        if (50 < yk)
          throw yk = 0, zk = null, Error(p(185));
        Ac(a, c, d);
        if (0 === (K & 2) || a !== Q)
          a === Q && (0 === (K & 2) && (qk |= c), 4 === T2 && Ck(a, Z)), Dk(a, d), 1 === c && 0 === K && 0 === (b.mode & 1) && (Gj = B() + 500, fg && jg());
      }
      function Dk(a, b) {
        var c = a.callbackNode;
        wc(a, b);
        var d = uc(a, a === Q ? Z : 0);
        if (0 === d)
          null !== c && bc(c), a.callbackNode = null, a.callbackPriority = 0;
        else if (b = d & -d, a.callbackPriority !== b) {
          null != c && bc(c);
          if (1 === b)
            0 === a.tag ? ig(Ek.bind(null, a)) : hg(Ek.bind(null, a)), Jf(function() {
              0 === (K & 6) && jg();
            }), c = null;
          else {
            switch (Dc(d)) {
              case 1:
                c = fc;
                break;
              case 4:
                c = gc;
                break;
              case 16:
                c = hc;
                break;
              case 536870912:
                c = jc;
                break;
              default:
                c = hc;
            }
            c = Fk(c, Gk.bind(null, a));
          }
          a.callbackPriority = b;
          a.callbackNode = c;
        }
      }
      function Gk(a, b) {
        Ak = -1;
        Bk = 0;
        if (0 !== (K & 6))
          throw Error(p(327));
        var c = a.callbackNode;
        if (Hk() && a.callbackNode !== c)
          return null;
        var d = uc(a, a === Q ? Z : 0);
        if (0 === d)
          return null;
        if (0 !== (d & 30) || 0 !== (d & a.expiredLanes) || b)
          b = Ik(a, d);
        else {
          b = d;
          var e = K;
          K |= 2;
          var f = Jk();
          if (Q !== a || Z !== b)
            uk = null, Gj = B() + 500, Kk(a, b);
          do
            try {
              Lk();
              break;
            } catch (h) {
              Mk(a, h);
            }
          while (1);
          $g();
          mk.current = f;
          K = e;
          null !== Y ? b = 0 : (Q = null, Z = 0, b = T2);
        }
        if (0 !== b) {
          2 === b && (e = xc(a), 0 !== e && (d = e, b = Nk(a, e)));
          if (1 === b)
            throw c = pk, Kk(a, 0), Ck(a, d), Dk(a, B()), c;
          if (6 === b)
            Ck(a, d);
          else {
            e = a.current.alternate;
            if (0 === (d & 30) && !Ok(e) && (b = Ik(a, d), 2 === b && (f = xc(a), 0 !== f && (d = f, b = Nk(a, f))), 1 === b))
              throw c = pk, Kk(a, 0), Ck(a, d), Dk(a, B()), c;
            a.finishedWork = e;
            a.finishedLanes = d;
            switch (b) {
              case 0:
              case 1:
                throw Error(p(345));
              case 2:
                Pk(a, tk, uk);
                break;
              case 3:
                Ck(a, d);
                if ((d & 130023424) === d && (b = fk + 500 - B(), 10 < b)) {
                  if (0 !== uc(a, 0))
                    break;
                  e = a.suspendedLanes;
                  if ((e & d) !== d) {
                    R();
                    a.pingedLanes |= a.suspendedLanes & e;
                    break;
                  }
                  a.timeoutHandle = Ff(Pk.bind(null, a, tk, uk), b);
                  break;
                }
                Pk(a, tk, uk);
                break;
              case 4:
                Ck(a, d);
                if ((d & 4194240) === d)
                  break;
                b = a.eventTimes;
                for (e = -1; 0 < d; ) {
                  var g = 31 - oc(d);
                  f = 1 << g;
                  g = b[g];
                  g > e && (e = g);
                  d &= ~f;
                }
                d = e;
                d = B() - d;
                d = (120 > d ? 120 : 480 > d ? 480 : 1080 > d ? 1080 : 1920 > d ? 1920 : 3e3 > d ? 3e3 : 4320 > d ? 4320 : 1960 * lk(d / 1960)) - d;
                if (10 < d) {
                  a.timeoutHandle = Ff(Pk.bind(null, a, tk, uk), d);
                  break;
                }
                Pk(a, tk, uk);
                break;
              case 5:
                Pk(a, tk, uk);
                break;
              default:
                throw Error(p(329));
            }
          }
        }
        Dk(a, B());
        return a.callbackNode === c ? Gk.bind(null, a) : null;
      }
      function Nk(a, b) {
        var c = sk;
        a.current.memoizedState.isDehydrated && (Kk(a, b).flags |= 256);
        a = Ik(a, b);
        2 !== a && (b = tk, tk = c, null !== b && Fj(b));
        return a;
      }
      function Fj(a) {
        null === tk ? tk = a : tk.push.apply(tk, a);
      }
      function Ok(a) {
        for (var b = a; ; ) {
          if (b.flags & 16384) {
            var c = b.updateQueue;
            if (null !== c && (c = c.stores, null !== c))
              for (var d = 0; d < c.length; d++) {
                var e = c[d], f = e.getSnapshot;
                e = e.value;
                try {
                  if (!He(f(), e))
                    return false;
                } catch (g) {
                  return false;
                }
              }
          }
          c = b.child;
          if (b.subtreeFlags & 16384 && null !== c)
            c.return = b, b = c;
          else {
            if (b === a)
              break;
            for (; null === b.sibling; ) {
              if (null === b.return || b.return === a)
                return true;
              b = b.return;
            }
            b.sibling.return = b.return;
            b = b.sibling;
          }
        }
        return true;
      }
      function Ck(a, b) {
        b &= ~rk;
        b &= ~qk;
        a.suspendedLanes |= b;
        a.pingedLanes &= ~b;
        for (a = a.expirationTimes; 0 < b; ) {
          var c = 31 - oc(b), d = 1 << c;
          a[c] = -1;
          b &= ~d;
        }
      }
      function Ek(a) {
        if (0 !== (K & 6))
          throw Error(p(327));
        Hk();
        var b = uc(a, 0);
        if (0 === (b & 1))
          return Dk(a, B()), null;
        var c = Ik(a, b);
        if (0 !== a.tag && 2 === c) {
          var d = xc(a);
          0 !== d && (b = d, c = Nk(a, d));
        }
        if (1 === c)
          throw c = pk, Kk(a, 0), Ck(a, b), Dk(a, B()), c;
        if (6 === c)
          throw Error(p(345));
        a.finishedWork = a.current.alternate;
        a.finishedLanes = b;
        Pk(a, tk, uk);
        Dk(a, B());
        return null;
      }
      function Qk(a, b) {
        var c = K;
        K |= 1;
        try {
          return a(b);
        } finally {
          K = c, 0 === K && (Gj = B() + 500, fg && jg());
        }
      }
      function Rk(a) {
        null !== wk && 0 === wk.tag && 0 === (K & 6) && Hk();
        var b = K;
        K |= 1;
        var c = ok.transition, d = C3;
        try {
          if (ok.transition = null, C3 = 1, a)
            return a();
        } finally {
          C3 = d, ok.transition = c, K = b, 0 === (K & 6) && jg();
        }
      }
      function Hj() {
        fj = ej.current;
        E(ej);
      }
      function Kk(a, b) {
        a.finishedWork = null;
        a.finishedLanes = 0;
        var c = a.timeoutHandle;
        -1 !== c && (a.timeoutHandle = -1, Gf(c));
        if (null !== Y)
          for (c = Y.return; null !== c; ) {
            var d = c;
            wg(d);
            switch (d.tag) {
              case 1:
                d = d.type.childContextTypes;
                null !== d && void 0 !== d && $f();
                break;
              case 3:
                zh();
                E(Wf);
                E(H12);
                Eh();
                break;
              case 5:
                Bh(d);
                break;
              case 4:
                zh();
                break;
              case 13:
                E(L);
                break;
              case 19:
                E(L);
                break;
              case 10:
                ah(d.type._context);
                break;
              case 22:
              case 23:
                Hj();
            }
            c = c.return;
          }
        Q = a;
        Y = a = Pg(a.current, null);
        Z = fj = b;
        T2 = 0;
        pk = null;
        rk = qk = rh = 0;
        tk = sk = null;
        if (null !== fh) {
          for (b = 0; b < fh.length; b++)
            if (c = fh[b], d = c.interleaved, null !== d) {
              c.interleaved = null;
              var e = d.next, f = c.pending;
              if (null !== f) {
                var g = f.next;
                f.next = e;
                d.next = g;
              }
              c.pending = d;
            }
          fh = null;
        }
        return a;
      }
      function Mk(a, b) {
        do {
          var c = Y;
          try {
            $g();
            Fh.current = Rh;
            if (Ih) {
              for (var d = M.memoizedState; null !== d; ) {
                var e = d.queue;
                null !== e && (e.pending = null);
                d = d.next;
              }
              Ih = false;
            }
            Hh = 0;
            O2 = N = M = null;
            Jh = false;
            Kh = 0;
            nk.current = null;
            if (null === c || null === c.return) {
              T2 = 1;
              pk = b;
              Y = null;
              break;
            }
            a: {
              var f = a, g = c.return, h = c, k = b;
              b = Z;
              h.flags |= 32768;
              if (null !== k && "object" === typeof k && "function" === typeof k.then) {
                var l = k, m = h, q = m.tag;
                if (0 === (m.mode & 1) && (0 === q || 11 === q || 15 === q)) {
                  var r = m.alternate;
                  r ? (m.updateQueue = r.updateQueue, m.memoizedState = r.memoizedState, m.lanes = r.lanes) : (m.updateQueue = null, m.memoizedState = null);
                }
                var y = Ui(g);
                if (null !== y) {
                  y.flags &= -257;
                  Vi(y, g, h, f, b);
                  y.mode & 1 && Si(f, l, b);
                  b = y;
                  k = l;
                  var n = b.updateQueue;
                  if (null === n) {
                    var t = /* @__PURE__ */ new Set();
                    t.add(k);
                    b.updateQueue = t;
                  } else
                    n.add(k);
                  break a;
                } else {
                  if (0 === (b & 1)) {
                    Si(f, l, b);
                    tj();
                    break a;
                  }
                  k = Error(p(426));
                }
              } else if (I && h.mode & 1) {
                var J = Ui(g);
                if (null !== J) {
                  0 === (J.flags & 65536) && (J.flags |= 256);
                  Vi(J, g, h, f, b);
                  Jg(Ji(k, h));
                  break a;
                }
              }
              f = k = Ji(k, h);
              4 !== T2 && (T2 = 2);
              null === sk ? sk = [f] : sk.push(f);
              f = g;
              do {
                switch (f.tag) {
                  case 3:
                    f.flags |= 65536;
                    b &= -b;
                    f.lanes |= b;
                    var x = Ni(f, k, b);
                    ph(f, x);
                    break a;
                  case 1:
                    h = k;
                    var w = f.type, u = f.stateNode;
                    if (0 === (f.flags & 128) && ("function" === typeof w.getDerivedStateFromError || null !== u && "function" === typeof u.componentDidCatch && (null === Ri || !Ri.has(u)))) {
                      f.flags |= 65536;
                      b &= -b;
                      f.lanes |= b;
                      var F = Qi(f, h, b);
                      ph(f, F);
                      break a;
                    }
                }
                f = f.return;
              } while (null !== f);
            }
            Sk(c);
          } catch (na) {
            b = na;
            Y === c && null !== c && (Y = c = c.return);
            continue;
          }
          break;
        } while (1);
      }
      function Jk() {
        var a = mk.current;
        mk.current = Rh;
        return null === a ? Rh : a;
      }
      function tj() {
        if (0 === T2 || 3 === T2 || 2 === T2)
          T2 = 4;
        null === Q || 0 === (rh & 268435455) && 0 === (qk & 268435455) || Ck(Q, Z);
      }
      function Ik(a, b) {
        var c = K;
        K |= 2;
        var d = Jk();
        if (Q !== a || Z !== b)
          uk = null, Kk(a, b);
        do
          try {
            Tk();
            break;
          } catch (e) {
            Mk(a, e);
          }
        while (1);
        $g();
        K = c;
        mk.current = d;
        if (null !== Y)
          throw Error(p(261));
        Q = null;
        Z = 0;
        return T2;
      }
      function Tk() {
        for (; null !== Y; )
          Uk(Y);
      }
      function Lk() {
        for (; null !== Y && !cc(); )
          Uk(Y);
      }
      function Uk(a) {
        var b = Vk(a.alternate, a, fj);
        a.memoizedProps = a.pendingProps;
        null === b ? Sk(a) : Y = b;
        nk.current = null;
      }
      function Sk(a) {
        var b = a;
        do {
          var c = b.alternate;
          a = b.return;
          if (0 === (b.flags & 32768)) {
            if (c = Ej(c, b, fj), null !== c) {
              Y = c;
              return;
            }
          } else {
            c = Ij(c, b);
            if (null !== c) {
              c.flags &= 32767;
              Y = c;
              return;
            }
            if (null !== a)
              a.flags |= 32768, a.subtreeFlags = 0, a.deletions = null;
            else {
              T2 = 6;
              Y = null;
              return;
            }
          }
          b = b.sibling;
          if (null !== b) {
            Y = b;
            return;
          }
          Y = b = a;
        } while (null !== b);
        0 === T2 && (T2 = 5);
      }
      function Pk(a, b, c) {
        var d = C3, e = ok.transition;
        try {
          ok.transition = null, C3 = 1, Wk(a, b, c, d);
        } finally {
          ok.transition = e, C3 = d;
        }
        return null;
      }
      function Wk(a, b, c, d) {
        do
          Hk();
        while (null !== wk);
        if (0 !== (K & 6))
          throw Error(p(327));
        c = a.finishedWork;
        var e = a.finishedLanes;
        if (null === c)
          return null;
        a.finishedWork = null;
        a.finishedLanes = 0;
        if (c === a.current)
          throw Error(p(177));
        a.callbackNode = null;
        a.callbackPriority = 0;
        var f = c.lanes | c.childLanes;
        Bc(a, f);
        a === Q && (Y = Q = null, Z = 0);
        0 === (c.subtreeFlags & 2064) && 0 === (c.flags & 2064) || vk || (vk = true, Fk(hc, function() {
          Hk();
          return null;
        }));
        f = 0 !== (c.flags & 15990);
        if (0 !== (c.subtreeFlags & 15990) || f) {
          f = ok.transition;
          ok.transition = null;
          var g = C3;
          C3 = 1;
          var h = K;
          K |= 4;
          nk.current = null;
          Oj(a, c);
          dk(c, a);
          Oe(Df);
          dd = !!Cf;
          Df = Cf = null;
          a.current = c;
          hk(c, a, e);
          dc();
          K = h;
          C3 = g;
          ok.transition = f;
        } else
          a.current = c;
        vk && (vk = false, wk = a, xk = e);
        f = a.pendingLanes;
        0 === f && (Ri = null);
        mc(c.stateNode, d);
        Dk(a, B());
        if (null !== b)
          for (d = a.onRecoverableError, c = 0; c < b.length; c++)
            e = b[c], d(e.value, { componentStack: e.stack, digest: e.digest });
        if (Oi)
          throw Oi = false, a = Pi, Pi = null, a;
        0 !== (xk & 1) && 0 !== a.tag && Hk();
        f = a.pendingLanes;
        0 !== (f & 1) ? a === zk ? yk++ : (yk = 0, zk = a) : yk = 0;
        jg();
        return null;
      }
      function Hk() {
        if (null !== wk) {
          var a = Dc(xk), b = ok.transition, c = C3;
          try {
            ok.transition = null;
            C3 = 16 > a ? 16 : a;
            if (null === wk)
              var d = false;
            else {
              a = wk;
              wk = null;
              xk = 0;
              if (0 !== (K & 6))
                throw Error(p(331));
              var e = K;
              K |= 4;
              for (V = a.current; null !== V; ) {
                var f = V, g = f.child;
                if (0 !== (V.flags & 16)) {
                  var h = f.deletions;
                  if (null !== h) {
                    for (var k = 0; k < h.length; k++) {
                      var l = h[k];
                      for (V = l; null !== V; ) {
                        var m = V;
                        switch (m.tag) {
                          case 0:
                          case 11:
                          case 15:
                            Pj(8, m, f);
                        }
                        var q = m.child;
                        if (null !== q)
                          q.return = m, V = q;
                        else
                          for (; null !== V; ) {
                            m = V;
                            var r = m.sibling, y = m.return;
                            Sj(m);
                            if (m === l) {
                              V = null;
                              break;
                            }
                            if (null !== r) {
                              r.return = y;
                              V = r;
                              break;
                            }
                            V = y;
                          }
                      }
                    }
                    var n = f.alternate;
                    if (null !== n) {
                      var t = n.child;
                      if (null !== t) {
                        n.child = null;
                        do {
                          var J = t.sibling;
                          t.sibling = null;
                          t = J;
                        } while (null !== t);
                      }
                    }
                    V = f;
                  }
                }
                if (0 !== (f.subtreeFlags & 2064) && null !== g)
                  g.return = f, V = g;
                else
                  b:
                    for (; null !== V; ) {
                      f = V;
                      if (0 !== (f.flags & 2048))
                        switch (f.tag) {
                          case 0:
                          case 11:
                          case 15:
                            Pj(9, f, f.return);
                        }
                      var x = f.sibling;
                      if (null !== x) {
                        x.return = f.return;
                        V = x;
                        break b;
                      }
                      V = f.return;
                    }
              }
              var w = a.current;
              for (V = w; null !== V; ) {
                g = V;
                var u = g.child;
                if (0 !== (g.subtreeFlags & 2064) && null !== u)
                  u.return = g, V = u;
                else
                  b:
                    for (g = w; null !== V; ) {
                      h = V;
                      if (0 !== (h.flags & 2048))
                        try {
                          switch (h.tag) {
                            case 0:
                            case 11:
                            case 15:
                              Qj(9, h);
                          }
                        } catch (na) {
                          W12(h, h.return, na);
                        }
                      if (h === g) {
                        V = null;
                        break b;
                      }
                      var F = h.sibling;
                      if (null !== F) {
                        F.return = h.return;
                        V = F;
                        break b;
                      }
                      V = h.return;
                    }
              }
              K = e;
              jg();
              if (lc && "function" === typeof lc.onPostCommitFiberRoot)
                try {
                  lc.onPostCommitFiberRoot(kc, a);
                } catch (na) {
                }
              d = true;
            }
            return d;
          } finally {
            C3 = c, ok.transition = b;
          }
        }
        return false;
      }
      function Xk(a, b, c) {
        b = Ji(c, b);
        b = Ni(a, b, 1);
        a = nh(a, b, 1);
        b = R();
        null !== a && (Ac(a, 1, b), Dk(a, b));
      }
      function W12(a, b, c) {
        if (3 === a.tag)
          Xk(a, a, c);
        else
          for (; null !== b; ) {
            if (3 === b.tag) {
              Xk(b, a, c);
              break;
            } else if (1 === b.tag) {
              var d = b.stateNode;
              if ("function" === typeof b.type.getDerivedStateFromError || "function" === typeof d.componentDidCatch && (null === Ri || !Ri.has(d))) {
                a = Ji(c, a);
                a = Qi(b, a, 1);
                b = nh(b, a, 1);
                a = R();
                null !== b && (Ac(b, 1, a), Dk(b, a));
                break;
              }
            }
            b = b.return;
          }
      }
      function Ti(a, b, c) {
        var d = a.pingCache;
        null !== d && d.delete(b);
        b = R();
        a.pingedLanes |= a.suspendedLanes & c;
        Q === a && (Z & c) === c && (4 === T2 || 3 === T2 && (Z & 130023424) === Z && 500 > B() - fk ? Kk(a, 0) : rk |= c);
        Dk(a, b);
      }
      function Yk(a, b) {
        0 === b && (0 === (a.mode & 1) ? b = 1 : (b = sc, sc <<= 1, 0 === (sc & 130023424) && (sc = 4194304)));
        var c = R();
        a = ih(a, b);
        null !== a && (Ac(a, b, c), Dk(a, c));
      }
      function uj(a) {
        var b = a.memoizedState, c = 0;
        null !== b && (c = b.retryLane);
        Yk(a, c);
      }
      function bk(a, b) {
        var c = 0;
        switch (a.tag) {
          case 13:
            var d = a.stateNode;
            var e = a.memoizedState;
            null !== e && (c = e.retryLane);
            break;
          case 19:
            d = a.stateNode;
            break;
          default:
            throw Error(p(314));
        }
        null !== d && d.delete(b);
        Yk(a, c);
      }
      var Vk;
      Vk = function(a, b, c) {
        if (null !== a)
          if (a.memoizedProps !== b.pendingProps || Wf.current)
            dh = true;
          else {
            if (0 === (a.lanes & c) && 0 === (b.flags & 128))
              return dh = false, yj(a, b, c);
            dh = 0 !== (a.flags & 131072) ? true : false;
          }
        else
          dh = false, I && 0 !== (b.flags & 1048576) && ug(b, ng, b.index);
        b.lanes = 0;
        switch (b.tag) {
          case 2:
            var d = b.type;
            ij(a, b);
            a = b.pendingProps;
            var e = Yf(b, H12.current);
            ch(b, c);
            e = Nh(null, b, d, a, e, c);
            var f = Sh();
            b.flags |= 1;
            "object" === typeof e && null !== e && "function" === typeof e.render && void 0 === e.$$typeof ? (b.tag = 1, b.memoizedState = null, b.updateQueue = null, Zf(d) ? (f = true, cg(b)) : f = false, b.memoizedState = null !== e.state && void 0 !== e.state ? e.state : null, kh(b), e.updater = Ei, b.stateNode = e, e._reactInternals = b, Ii(b, d, a, c), b = jj(null, b, d, true, f, c)) : (b.tag = 0, I && f && vg(b), Xi(null, b, e, c), b = b.child);
            return b;
          case 16:
            d = b.elementType;
            a: {
              ij(a, b);
              a = b.pendingProps;
              e = d._init;
              d = e(d._payload);
              b.type = d;
              e = b.tag = Zk(d);
              a = Ci(d, a);
              switch (e) {
                case 0:
                  b = cj(null, b, d, a, c);
                  break a;
                case 1:
                  b = hj(null, b, d, a, c);
                  break a;
                case 11:
                  b = Yi(null, b, d, a, c);
                  break a;
                case 14:
                  b = $i(null, b, d, Ci(d.type, a), c);
                  break a;
              }
              throw Error(p(
                306,
                d,
                ""
              ));
            }
            return b;
          case 0:
            return d = b.type, e = b.pendingProps, e = b.elementType === d ? e : Ci(d, e), cj(a, b, d, e, c);
          case 1:
            return d = b.type, e = b.pendingProps, e = b.elementType === d ? e : Ci(d, e), hj(a, b, d, e, c);
          case 3:
            a: {
              kj(b);
              if (null === a)
                throw Error(p(387));
              d = b.pendingProps;
              f = b.memoizedState;
              e = f.element;
              lh(a, b);
              qh(b, d, null, c);
              var g = b.memoizedState;
              d = g.element;
              if (f.isDehydrated)
                if (f = { element: d, isDehydrated: false, cache: g.cache, pendingSuspenseBoundaries: g.pendingSuspenseBoundaries, transitions: g.transitions }, b.updateQueue.baseState = f, b.memoizedState = f, b.flags & 256) {
                  e = Ji(Error(p(423)), b);
                  b = lj(a, b, d, c, e);
                  break a;
                } else if (d !== e) {
                  e = Ji(Error(p(424)), b);
                  b = lj(a, b, d, c, e);
                  break a;
                } else
                  for (yg = Lf(b.stateNode.containerInfo.firstChild), xg = b, I = true, zg = null, c = Vg(b, null, d, c), b.child = c; c; )
                    c.flags = c.flags & -3 | 4096, c = c.sibling;
              else {
                Ig();
                if (d === e) {
                  b = Zi(a, b, c);
                  break a;
                }
                Xi(a, b, d, c);
              }
              b = b.child;
            }
            return b;
          case 5:
            return Ah(b), null === a && Eg(b), d = b.type, e = b.pendingProps, f = null !== a ? a.memoizedProps : null, g = e.children, Ef(d, e) ? g = null : null !== f && Ef(d, f) && (b.flags |= 32), gj(a, b), Xi(a, b, g, c), b.child;
          case 6:
            return null === a && Eg(b), null;
          case 13:
            return oj(a, b, c);
          case 4:
            return yh(b, b.stateNode.containerInfo), d = b.pendingProps, null === a ? b.child = Ug(b, null, d, c) : Xi(a, b, d, c), b.child;
          case 11:
            return d = b.type, e = b.pendingProps, e = b.elementType === d ? e : Ci(d, e), Yi(a, b, d, e, c);
          case 7:
            return Xi(a, b, b.pendingProps, c), b.child;
          case 8:
            return Xi(a, b, b.pendingProps.children, c), b.child;
          case 12:
            return Xi(a, b, b.pendingProps.children, c), b.child;
          case 10:
            a: {
              d = b.type._context;
              e = b.pendingProps;
              f = b.memoizedProps;
              g = e.value;
              G4(Wg, d._currentValue);
              d._currentValue = g;
              if (null !== f)
                if (He(f.value, g)) {
                  if (f.children === e.children && !Wf.current) {
                    b = Zi(a, b, c);
                    break a;
                  }
                } else
                  for (f = b.child, null !== f && (f.return = b); null !== f; ) {
                    var h = f.dependencies;
                    if (null !== h) {
                      g = f.child;
                      for (var k = h.firstContext; null !== k; ) {
                        if (k.context === d) {
                          if (1 === f.tag) {
                            k = mh(-1, c & -c);
                            k.tag = 2;
                            var l = f.updateQueue;
                            if (null !== l) {
                              l = l.shared;
                              var m = l.pending;
                              null === m ? k.next = k : (k.next = m.next, m.next = k);
                              l.pending = k;
                            }
                          }
                          f.lanes |= c;
                          k = f.alternate;
                          null !== k && (k.lanes |= c);
                          bh(
                            f.return,
                            c,
                            b
                          );
                          h.lanes |= c;
                          break;
                        }
                        k = k.next;
                      }
                    } else if (10 === f.tag)
                      g = f.type === b.type ? null : f.child;
                    else if (18 === f.tag) {
                      g = f.return;
                      if (null === g)
                        throw Error(p(341));
                      g.lanes |= c;
                      h = g.alternate;
                      null !== h && (h.lanes |= c);
                      bh(g, c, b);
                      g = f.sibling;
                    } else
                      g = f.child;
                    if (null !== g)
                      g.return = f;
                    else
                      for (g = f; null !== g; ) {
                        if (g === b) {
                          g = null;
                          break;
                        }
                        f = g.sibling;
                        if (null !== f) {
                          f.return = g.return;
                          g = f;
                          break;
                        }
                        g = g.return;
                      }
                    f = g;
                  }
              Xi(a, b, e.children, c);
              b = b.child;
            }
            return b;
          case 9:
            return e = b.type, d = b.pendingProps.children, ch(b, c), e = eh(e), d = d(e), b.flags |= 1, Xi(a, b, d, c), b.child;
          case 14:
            return d = b.type, e = Ci(d, b.pendingProps), e = Ci(d.type, e), $i(a, b, d, e, c);
          case 15:
            return bj(a, b, b.type, b.pendingProps, c);
          case 17:
            return d = b.type, e = b.pendingProps, e = b.elementType === d ? e : Ci(d, e), ij(a, b), b.tag = 1, Zf(d) ? (a = true, cg(b)) : a = false, ch(b, c), Gi(b, d, e), Ii(b, d, e, c), jj(null, b, d, true, a, c);
          case 19:
            return xj(a, b, c);
          case 22:
            return dj(a, b, c);
        }
        throw Error(p(156, b.tag));
      };
      function Fk(a, b) {
        return ac(a, b);
      }
      function $k(a, b, c, d) {
        this.tag = a;
        this.key = c;
        this.sibling = this.child = this.return = this.stateNode = this.type = this.elementType = null;
        this.index = 0;
        this.ref = null;
        this.pendingProps = b;
        this.dependencies = this.memoizedState = this.updateQueue = this.memoizedProps = null;
        this.mode = d;
        this.subtreeFlags = this.flags = 0;
        this.deletions = null;
        this.childLanes = this.lanes = 0;
        this.alternate = null;
      }
      function Bg(a, b, c, d) {
        return new $k(a, b, c, d);
      }
      function aj(a) {
        a = a.prototype;
        return !(!a || !a.isReactComponent);
      }
      function Zk(a) {
        if ("function" === typeof a)
          return aj(a) ? 1 : 0;
        if (void 0 !== a && null !== a) {
          a = a.$$typeof;
          if (a === Da)
            return 11;
          if (a === Ga)
            return 14;
        }
        return 2;
      }
      function Pg(a, b) {
        var c = a.alternate;
        null === c ? (c = Bg(a.tag, b, a.key, a.mode), c.elementType = a.elementType, c.type = a.type, c.stateNode = a.stateNode, c.alternate = a, a.alternate = c) : (c.pendingProps = b, c.type = a.type, c.flags = 0, c.subtreeFlags = 0, c.deletions = null);
        c.flags = a.flags & 14680064;
        c.childLanes = a.childLanes;
        c.lanes = a.lanes;
        c.child = a.child;
        c.memoizedProps = a.memoizedProps;
        c.memoizedState = a.memoizedState;
        c.updateQueue = a.updateQueue;
        b = a.dependencies;
        c.dependencies = null === b ? null : { lanes: b.lanes, firstContext: b.firstContext };
        c.sibling = a.sibling;
        c.index = a.index;
        c.ref = a.ref;
        return c;
      }
      function Rg(a, b, c, d, e, f) {
        var g = 2;
        d = a;
        if ("function" === typeof a)
          aj(a) && (g = 1);
        else if ("string" === typeof a)
          g = 5;
        else
          a:
            switch (a) {
              case ya:
                return Tg(c.children, e, f, b);
              case za:
                g = 8;
                e |= 8;
                break;
              case Aa:
                return a = Bg(12, c, b, e | 2), a.elementType = Aa, a.lanes = f, a;
              case Ea:
                return a = Bg(13, c, b, e), a.elementType = Ea, a.lanes = f, a;
              case Fa:
                return a = Bg(19, c, b, e), a.elementType = Fa, a.lanes = f, a;
              case Ia:
                return pj(c, e, f, b);
              default:
                if ("object" === typeof a && null !== a)
                  switch (a.$$typeof) {
                    case Ba:
                      g = 10;
                      break a;
                    case Ca:
                      g = 9;
                      break a;
                    case Da:
                      g = 11;
                      break a;
                    case Ga:
                      g = 14;
                      break a;
                    case Ha:
                      g = 16;
                      d = null;
                      break a;
                  }
                throw Error(p(130, null == a ? a : typeof a, ""));
            }
        b = Bg(g, c, b, e);
        b.elementType = a;
        b.type = d;
        b.lanes = f;
        return b;
      }
      function Tg(a, b, c, d) {
        a = Bg(7, a, d, b);
        a.lanes = c;
        return a;
      }
      function pj(a, b, c, d) {
        a = Bg(22, a, d, b);
        a.elementType = Ia;
        a.lanes = c;
        a.stateNode = { isHidden: false };
        return a;
      }
      function Qg(a, b, c) {
        a = Bg(6, a, null, b);
        a.lanes = c;
        return a;
      }
      function Sg(a, b, c) {
        b = Bg(4, null !== a.children ? a.children : [], a.key, b);
        b.lanes = c;
        b.stateNode = { containerInfo: a.containerInfo, pendingChildren: null, implementation: a.implementation };
        return b;
      }
      function al(a, b, c, d, e) {
        this.tag = b;
        this.containerInfo = a;
        this.finishedWork = this.pingCache = this.current = this.pendingChildren = null;
        this.timeoutHandle = -1;
        this.callbackNode = this.pendingContext = this.context = null;
        this.callbackPriority = 0;
        this.eventTimes = zc(0);
        this.expirationTimes = zc(-1);
        this.entangledLanes = this.finishedLanes = this.mutableReadLanes = this.expiredLanes = this.pingedLanes = this.suspendedLanes = this.pendingLanes = 0;
        this.entanglements = zc(0);
        this.identifierPrefix = d;
        this.onRecoverableError = e;
        this.mutableSourceEagerHydrationData = null;
      }
      function bl(a, b, c, d, e, f, g, h, k) {
        a = new al(a, b, c, h, k);
        1 === b ? (b = 1, true === f && (b |= 8)) : b = 0;
        f = Bg(3, null, null, b);
        a.current = f;
        f.stateNode = a;
        f.memoizedState = { element: d, isDehydrated: c, cache: null, transitions: null, pendingSuspenseBoundaries: null };
        kh(f);
        return a;
      }
      function cl(a, b, c) {
        var d = 3 < arguments.length && void 0 !== arguments[3] ? arguments[3] : null;
        return { $$typeof: wa, key: null == d ? null : "" + d, children: a, containerInfo: b, implementation: c };
      }
      function dl(a) {
        if (!a)
          return Vf;
        a = a._reactInternals;
        a: {
          if (Vb(a) !== a || 1 !== a.tag)
            throw Error(p(170));
          var b = a;
          do {
            switch (b.tag) {
              case 3:
                b = b.stateNode.context;
                break a;
              case 1:
                if (Zf(b.type)) {
                  b = b.stateNode.__reactInternalMemoizedMergedChildContext;
                  break a;
                }
            }
            b = b.return;
          } while (null !== b);
          throw Error(p(171));
        }
        if (1 === a.tag) {
          var c = a.type;
          if (Zf(c))
            return bg(a, c, b);
        }
        return b;
      }
      function el(a, b, c, d, e, f, g, h, k) {
        a = bl(c, d, true, a, e, f, g, h, k);
        a.context = dl(null);
        c = a.current;
        d = R();
        e = yi(c);
        f = mh(d, e);
        f.callback = void 0 !== b && null !== b ? b : null;
        nh(c, f, e);
        a.current.lanes = e;
        Ac(a, e, d);
        Dk(a, d);
        return a;
      }
      function fl(a, b, c, d) {
        var e = b.current, f = R(), g = yi(e);
        c = dl(c);
        null === b.context ? b.context = c : b.pendingContext = c;
        b = mh(f, g);
        b.payload = { element: a };
        d = void 0 === d ? null : d;
        null !== d && (b.callback = d);
        a = nh(e, b, g);
        null !== a && (gi(a, e, g, f), oh(a, e, g));
        return g;
      }
      function gl(a) {
        a = a.current;
        if (!a.child)
          return null;
        switch (a.child.tag) {
          case 5:
            return a.child.stateNode;
          default:
            return a.child.stateNode;
        }
      }
      function hl(a, b) {
        a = a.memoizedState;
        if (null !== a && null !== a.dehydrated) {
          var c = a.retryLane;
          a.retryLane = 0 !== c && c < b ? c : b;
        }
      }
      function il(a, b) {
        hl(a, b);
        (a = a.alternate) && hl(a, b);
      }
      function jl() {
        return null;
      }
      var kl = "function" === typeof reportError ? reportError : function(a) {
        console.error(a);
      };
      function ll(a) {
        this._internalRoot = a;
      }
      ml.prototype.render = ll.prototype.render = function(a) {
        var b = this._internalRoot;
        if (null === b)
          throw Error(p(409));
        fl(a, b, null, null);
      };
      ml.prototype.unmount = ll.prototype.unmount = function() {
        var a = this._internalRoot;
        if (null !== a) {
          this._internalRoot = null;
          var b = a.containerInfo;
          Rk(function() {
            fl(null, a, null, null);
          });
          b[uf] = null;
        }
      };
      function ml(a) {
        this._internalRoot = a;
      }
      ml.prototype.unstable_scheduleHydration = function(a) {
        if (a) {
          var b = Hc();
          a = { blockedOn: null, target: a, priority: b };
          for (var c = 0; c < Qc.length && 0 !== b && b < Qc[c].priority; c++)
            ;
          Qc.splice(c, 0, a);
          0 === c && Vc(a);
        }
      };
      function nl(a) {
        return !(!a || 1 !== a.nodeType && 9 !== a.nodeType && 11 !== a.nodeType);
      }
      function ol(a) {
        return !(!a || 1 !== a.nodeType && 9 !== a.nodeType && 11 !== a.nodeType && (8 !== a.nodeType || " react-mount-point-unstable " !== a.nodeValue));
      }
      function pl() {
      }
      function ql(a, b, c, d, e) {
        if (e) {
          if ("function" === typeof d) {
            var f = d;
            d = function() {
              var a2 = gl(g);
              f.call(a2);
            };
          }
          var g = el(b, d, a, 0, null, false, false, "", pl);
          a._reactRootContainer = g;
          a[uf] = g.current;
          sf(8 === a.nodeType ? a.parentNode : a);
          Rk();
          return g;
        }
        for (; e = a.lastChild; )
          a.removeChild(e);
        if ("function" === typeof d) {
          var h = d;
          d = function() {
            var a2 = gl(k);
            h.call(a2);
          };
        }
        var k = bl(a, 0, false, null, null, false, false, "", pl);
        a._reactRootContainer = k;
        a[uf] = k.current;
        sf(8 === a.nodeType ? a.parentNode : a);
        Rk(function() {
          fl(b, k, c, d);
        });
        return k;
      }
      function rl(a, b, c, d, e) {
        var f = c._reactRootContainer;
        if (f) {
          var g = f;
          if ("function" === typeof e) {
            var h = e;
            e = function() {
              var a2 = gl(g);
              h.call(a2);
            };
          }
          fl(b, g, a, e);
        } else
          g = ql(c, b, a, e, d);
        return gl(g);
      }
      Ec = function(a) {
        switch (a.tag) {
          case 3:
            var b = a.stateNode;
            if (b.current.memoizedState.isDehydrated) {
              var c = tc(b.pendingLanes);
              0 !== c && (Cc(b, c | 1), Dk(b, B()), 0 === (K & 6) && (Gj = B() + 500, jg()));
            }
            break;
          case 13:
            Rk(function() {
              var b2 = ih(a, 1);
              if (null !== b2) {
                var c2 = R();
                gi(b2, a, 1, c2);
              }
            }), il(a, 1);
        }
      };
      Fc = function(a) {
        if (13 === a.tag) {
          var b = ih(a, 134217728);
          if (null !== b) {
            var c = R();
            gi(b, a, 134217728, c);
          }
          il(a, 134217728);
        }
      };
      Gc = function(a) {
        if (13 === a.tag) {
          var b = yi(a), c = ih(a, b);
          if (null !== c) {
            var d = R();
            gi(c, a, b, d);
          }
          il(a, b);
        }
      };
      Hc = function() {
        return C3;
      };
      Ic = function(a, b) {
        var c = C3;
        try {
          return C3 = a, b();
        } finally {
          C3 = c;
        }
      };
      yb = function(a, b, c) {
        switch (b) {
          case "input":
            bb(a, c);
            b = c.name;
            if ("radio" === c.type && null != b) {
              for (c = a; c.parentNode; )
                c = c.parentNode;
              c = c.querySelectorAll("input[name=" + JSON.stringify("" + b) + '][type="radio"]');
              for (b = 0; b < c.length; b++) {
                var d = c[b];
                if (d !== a && d.form === a.form) {
                  var e = Db(d);
                  if (!e)
                    throw Error(p(90));
                  Wa(d);
                  bb(d, e);
                }
              }
            }
            break;
          case "textarea":
            ib(a, c);
            break;
          case "select":
            b = c.value, null != b && fb(a, !!c.multiple, b, false);
        }
      };
      Gb = Qk;
      Hb = Rk;
      var sl = { usingClientEntryPoint: false, Events: [Cb, ue, Db, Eb, Fb, Qk] };
      var tl = { findFiberByHostInstance: Wc, bundleType: 0, version: "18.3.1", rendererPackageName: "react-dom" };
      var ul = { bundleType: tl.bundleType, version: tl.version, rendererPackageName: tl.rendererPackageName, rendererConfig: tl.rendererConfig, overrideHookState: null, overrideHookStateDeletePath: null, overrideHookStateRenamePath: null, overrideProps: null, overridePropsDeletePath: null, overridePropsRenamePath: null, setErrorHandler: null, setSuspenseHandler: null, scheduleUpdate: null, currentDispatcherRef: ua.ReactCurrentDispatcher, findHostInstanceByFiber: function(a) {
        a = Zb(a);
        return null === a ? null : a.stateNode;
      }, findFiberByHostInstance: tl.findFiberByHostInstance || jl, findHostInstancesForRefresh: null, scheduleRefresh: null, scheduleRoot: null, setRefreshHandler: null, getCurrentFiber: null, reconcilerVersion: "18.3.1-next-f1338f8080-20240426" };
      if ("undefined" !== typeof __REACT_DEVTOOLS_GLOBAL_HOOK__) {
        vl = __REACT_DEVTOOLS_GLOBAL_HOOK__;
        if (!vl.isDisabled && vl.supportsFiber)
          try {
            kc = vl.inject(ul), lc = vl;
          } catch (a) {
          }
      }
      var vl;
      exports.__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED = sl;
      exports.createPortal = function(a, b) {
        var c = 2 < arguments.length && void 0 !== arguments[2] ? arguments[2] : null;
        if (!nl(b))
          throw Error(p(200));
        return cl(a, b, null, c);
      };
      exports.createRoot = function(a, b) {
        if (!nl(a))
          throw Error(p(299));
        var c = false, d = "", e = kl;
        null !== b && void 0 !== b && (true === b.unstable_strictMode && (c = true), void 0 !== b.identifierPrefix && (d = b.identifierPrefix), void 0 !== b.onRecoverableError && (e = b.onRecoverableError));
        b = bl(a, 1, false, null, null, c, false, d, e);
        a[uf] = b.current;
        sf(8 === a.nodeType ? a.parentNode : a);
        return new ll(b);
      };
      exports.findDOMNode = function(a) {
        if (null == a)
          return null;
        if (1 === a.nodeType)
          return a;
        var b = a._reactInternals;
        if (void 0 === b) {
          if ("function" === typeof a.render)
            throw Error(p(188));
          a = Object.keys(a).join(",");
          throw Error(p(268, a));
        }
        a = Zb(b);
        a = null === a ? null : a.stateNode;
        return a;
      };
      exports.flushSync = function(a) {
        return Rk(a);
      };
      exports.hydrate = function(a, b, c) {
        if (!ol(b))
          throw Error(p(200));
        return rl(null, a, b, true, c);
      };
      exports.hydrateRoot = function(a, b, c) {
        if (!nl(a))
          throw Error(p(405));
        var d = null != c && c.hydratedSources || null, e = false, f = "", g = kl;
        null !== c && void 0 !== c && (true === c.unstable_strictMode && (e = true), void 0 !== c.identifierPrefix && (f = c.identifierPrefix), void 0 !== c.onRecoverableError && (g = c.onRecoverableError));
        b = el(b, null, a, 1, null != c ? c : null, e, false, f, g);
        a[uf] = b.current;
        sf(a);
        if (d)
          for (a = 0; a < d.length; a++)
            c = d[a], e = c._getVersion, e = e(c._source), null == b.mutableSourceEagerHydrationData ? b.mutableSourceEagerHydrationData = [c, e] : b.mutableSourceEagerHydrationData.push(
              c,
              e
            );
        return new ml(b);
      };
      exports.render = function(a, b, c) {
        if (!ol(b))
          throw Error(p(200));
        return rl(null, a, b, false, c);
      };
      exports.unmountComponentAtNode = function(a) {
        if (!ol(a))
          throw Error(p(40));
        return a._reactRootContainer ? (Rk(function() {
          rl(null, null, a, false, function() {
            a._reactRootContainer = null;
            a[uf] = null;
          });
        }), true) : false;
      };
      exports.unstable_batchedUpdates = Qk;
      exports.unstable_renderSubtreeIntoContainer = function(a, b, c, d) {
        if (!ol(c))
          throw Error(p(200));
        if (null == a || void 0 === a._reactInternals)
          throw Error(p(38));
        return rl(a, b, c, false, d);
      };
      exports.version = "18.3.1-next-f1338f8080-20240426";
    }
  });

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react-dom/index.js
  var require_react_dom = __commonJS({
    "../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react-dom/index.js"(exports, module) {
      "use strict";
      function checkDCE() {
        if (typeof __REACT_DEVTOOLS_GLOBAL_HOOK__ === "undefined" || typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE !== "function") {
          return;
        }
        if (false) {
          throw new Error("^_^");
        }
        try {
          __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(checkDCE);
        } catch (err) {
          console.error(err);
        }
      }
      if (true) {
        checkDCE();
        module.exports = require_react_dom_production_min();
      } else {
        module.exports = null;
      }
    }
  });

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react-dom/client.js
  var require_client = __commonJS({
    "../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react-dom/client.js"(exports) {
      "use strict";
      var m = require_react_dom();
      if (true) {
        exports.createRoot = m.createRoot;
        exports.hydrateRoot = m.hydrateRoot;
      } else {
        i = m.__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED;
        exports.createRoot = function(c, o) {
          i.usingClientEntryPoint = true;
          try {
            return m.createRoot(c, o);
          } finally {
            i.usingClientEntryPoint = false;
          }
        };
        exports.hydrateRoot = function(c, h, o) {
          i.usingClientEntryPoint = true;
          try {
            return m.hydrateRoot(c, h, o);
          } finally {
            i.usingClientEntryPoint = false;
          }
        };
      }
      var i;
    }
  });

  // math/physics-catalog.js
  var require_physics_catalog = __commonJS({
    "math/physics-catalog.js"(exports, module) {
      "use strict";
      (function(root, factory) {
        const api = factory();
        if (typeof module === "object" && module.exports)
          module.exports = api;
        else
          root.NBPhysics = api;
      })(typeof window !== "undefined" ? window : globalThis, function() {
        "use strict";
        const groups = { mechanics: "C\u01A1 h\u1ECDc", oscillation: "Dao \u0111\u1ED9ng", waves: "S\xF3ng", electric: "\u0110i\u1EC7n h\u1ECDc", magnetic: "T\u1EEB tr\u01B0\u1EDDng & c\u1EA3m \u1EE9ng", thermal: "Nhi\u1EC7t h\u1ECDc", nuclear: "H\u1EA1t nh\xE2n" };
        const rows = [
          ["force-vector", "ForceVectorSimulation", "T\u1ED5ng h\u1EE3p & ph\xE2n t\xEDch l\u1EF1c", "mechanics", "K\xE9o l\u1EF1c, xoay tr\u1EE5c v\xE0 quan s\xE1t c\xE1c th\xE0nh ph\u1EA7n."],
          ["inclined-plane", "InclinedPlaneSimulation", "M\u1EB7t ph\u1EB3ng nghi\xEAng & ma s\xE1t", "mechanics", "K\xE9o th\u1EA3 v\u1EADt tr\xEAn d\u1ED1c v\xE0 m\u1EB7t ph\u1EB3ng ngang."],
          ["collision", "CollisionSimulation", "Va ch\u1EA1m & \u0111\u1ED9ng l\u01B0\u1EE3ng", "mechanics", "Thay \u0111\u1ED5i kh\u1ED1i l\u01B0\u1EE3ng, v\u1EADn t\u1ED1c v\xE0 h\u1EC7 s\u1ED1 ph\u1EE5c h\u1ED3i."],
          ["projectile", "ProjectileMotionSimulation", "Chuy\u1EC3n \u0111\u1ED9ng n\xE9m", "mechanics", "N\xE9m ngang, n\xE9m xi\xEAn, qu\u1EF9 \u0111\u1EA1o v\xE0 vect\u01A1 v\u1EADn t\u1ED1c.", 1],
          ["free-fall", "FreeFall3DSimulation", "R\u01A1i t\u1EF1 do", "mechanics", "Quan s\xE1t r\u01A1i t\u1EF1 do v\xE0 c\xE1c \u0111\u1ED3 th\u1ECB chuy\u1EC3n \u0111\u1ED9ng.", 1],
          ["circular-motion", "CircularMotionSimulation", "Chuy\u1EC3n \u0111\u1ED9ng tr\xF2n", "mechanics", "G\xF3c quay, v\u1EADn t\u1ED1c v\xE0 gia t\u1ED1c h\u01B0\u1EDBng t\xE2m."],
          ["motion-graphs", "MotionGraphsSimulation", "\u0110\u1ED3 th\u1ECB chuy\u1EC3n \u0111\u1ED9ng", "mechanics", "Li\xEAn h\u1EC7 chuy\u1EC3n \u0111\u1ED9ng v\u1EDBi \u0111\u1ED3 th\u1ECB."],
          ["displacement-time", "DisplacementTimeSimulation", "\u0110\u1ED9 d\u1ECBch chuy\u1EC3n \u2013 th\u1EDDi gian", "mechanics", "Chuy\u1EC3n \u0111\u1ED9ng v\xE0 \u0111\u1ED3 th\u1ECB x\u2013t."],
          ["vt-graph", "InteractiveVTGraph", "\u0110\u1ED3 th\u1ECB v\u1EADn t\u1ED1c \u2013 th\u1EDDi gian", "mechanics", "K\xE9o \u0111\u1ED3 th\u1ECB \u0111\u1EC3 thay \u0111\u1ED5i v\u1EADn t\u1ED1c \u0111\u1EA7u v\xE0 gia t\u1ED1c."],
          ["hooke-spring", "HookeSpringSimulation", "L\xF2 xo & \u0111\u1ECBnh lu\u1EADt Hooke", "mechanics", "\u0110\u1ED9 bi\u1EBFn d\u1EA1ng v\xE0 l\u1EF1c \u0111\xE0n h\u1ED3i."],
          ["spring-oscillator", "SpringOscillatorSimulation", "Con l\u1EAFc l\xF2 xo", "oscillation", "K\xE9o th\u1EA3 v\u1EADt; thay \u0111\u1ED5i m, k v\xE0 ma s\xE1t."],
          ["simple-pendulum", "SimplePendulumSimulation", "Con l\u1EAFc \u0111\u01A1n", "oscillation", "K\xE9o th\u1EA3 con l\u1EAFc, \u0111i\u1EC1u ch\u1EC9nh chi\u1EC1u d\xE0i v\xE0 l\u1EF1c c\u1EA3n."],
          ["energy-oscillation", "EnergyOscillationSimulation", "N\u0103ng l\u01B0\u1EE3ng dao \u0111\u1ED9ng", "oscillation", "\u0110\u1ED9ng n\u0103ng, th\u1EBF n\u0103ng v\xE0 c\u01A1 n\u0103ng."],
          ["two-oscillations", "TwoOscillationsSimulation", "\u0110\u1ED9 l\u1EC7ch pha c\u1EE7a hai dao \u0111\u1ED9ng", "oscillation", "Hai dao \u0111\u1ED9ng c\xF9ng t\u1EA7n s\u1ED1; thay \u0111\u1ED5i bi\xEAn \u0111\u1ED9, pha v\xE0 so s\xE1nh hai \u0111\u1ED3 th\u1ECB."],
          ["driven-resonance", "DrivenResonanceSimulation", "Dao \u0111\u1ED9ng c\u01B0\u1EE1ng b\u1EE9c & c\u1ED9ng h\u01B0\u1EDFng", "oscillation", "Thay \u0111\u1ED5i t\u1EA7n s\u1ED1 c\u01B0\u1EE1ng b\u1EE9c v\xE0 \u0111\u1ED9 c\u1EA3n."],
          ["wave-propagation", "WavePropagationSimulation", "S\u1EF1 truy\u1EC1n s\xF3ng", "waves", "Theo d\xF5i s\u1EF1 lan truy\u1EC1n dao \u0111\u1ED9ng."],
          ["wave-interference", "WaveInterferenceSimulation", "Giao thoa s\xF3ng", "waves", "Kho\u1EA3ng c\xE1ch ngu\u1ED3n v\xE0 b\u01B0\u1EDBc s\xF3ng."],
          ["standing-wave", "StandingWaveSimulation", "S\xF3ng d\u1EEBng", "waves", "N\xFAt, b\u1EE5ng s\xF3ng v\xE0 c\xE1c mode dao \u0111\u1ED9ng."],
          ["coulomb", "CoulombForceSimulation", "L\u1EF1c Coulomb", "electric", "K\xE9o \u0111i\u1EC7n t\xEDch v\xE0 \u0111\u1ED5i d\u1EA5u \u0111i\u1EC7n t\xEDch.", 0, true],
          ["electric-field", "ElectricFieldSimulation", "\u0110i\u1EC7n t\xEDch trong \u0111i\u1EC7n tr\u01B0\u1EDDng", "electric", "\u0110i\u1EC7n tr\u01B0\u1EDDng, \u0111i\u1EC7n t\xEDch v\xE0 chuy\u1EC3n \u0111\u1ED9ng.", 0, true],
          ["resistance-ohm", "ResistanceOhmSimulation", "\u0110i\u1EC7n tr\u1EDF & \u0111\u1ECBnh lu\u1EADt Ohm", "electric", "\u0110\u1EB7c tuy\u1EBFn \u0111i\u1EC7n tr\u1EDF, \u0111\xE8n s\u1EE3i \u0111\u1ED1t v\xE0 NTC."],
          ["circuit", "CircuitSimulation", "Ngu\u1ED3n \u0111i\u1EC7n & m\u1EA1ch k\xEDn", "electric", "\u0110\xF3ng m\u1EDF c\xF4ng t\u1EAFc, quan s\xE1t d\xF2ng \u0111i\u1EC7n v\xE0 \u0111\xE8n.", 1, true],
          ["emf-measurement", "EMFMeasurementSimulation", "\u0110o su\u1EA5t \u0111i\u1EC7n \u0111\u1ED9ng", "electric", "Kh\u1EA3o s\xE1t ngu\u1ED3n \u0111i\u1EC7n qua m\xF4 h\xECnh \u0111o."],
          ["magnetic-field", "MagneticFieldSimulation", "T\u1EEB tr\u01B0\u1EDDng", "magnetic", "Quan s\xE1t h\xECnh d\u1EA1ng t\u1EEB tr\u01B0\u1EDDng trong kh\xF4ng gian.", 1, true],
          ["magnetic-force", "MagneticForceSimulation", "L\u1EF1c t\u1EEB", "magnetic", "Thay \u0111\u1ED5i \u0111\u1EA1i l\u01B0\u1EE3ng v\xE0 g\xF3c \u0111\u1EB7t trong t\u1EEB tr\u01B0\u1EDDng.", 1],
          ["em-induction", "EMInductionSimulation", "C\u1EA3m \u1EE9ng \u0111i\u1EC7n t\u1EEB", "magnetic", "K\xE9o nam ch\xE2m qua cu\u1ED9n d\xE2y; quan s\xE1t chi\u1EC1u c\u1EA3m \u1EE9ng.", 1, true],
          ["ac-generator", "AcGeneratorSimulation", "M\xE1y ph\xE1t \u0111i\u1EC7n xoay chi\u1EC1u", "magnetic", "Khung d\xE2y quay, t\u1EEB th\xF4ng v\xE0 su\u1EA5t \u0111i\u1EC7n \u0111\u1ED9ng.", 1],
          ["gas-law", "GasLawSimulation", "C\xE1c \u0111\u1ECBnh lu\u1EADt ch\u1EA5t kh\xED", "thermal", "Thay \u0111\u1ED5i nhi\u1EC7t \u0111\u1ED9, \xE1p su\u1EA5t v\xE0 th\u1EC3 t\xEDch.", 0, true],
          ["gas-particles", "GasParticleSimulation", "Chuy\u1EC3n \u0111\u1ED9ng ph\xE2n t\u1EED kh\xED", "thermal", "Quan s\xE1t m\xF4 h\xECnh h\u1EA1t v\xE0 va ch\u1EA1m.", 0, true],
          ["heating-curve", "HeatingCurveSimulation", "\u0110\u01B0\u1EDDng \u0111un n\xF3ng", "thermal", "Nhi\u1EC7t l\u01B0\u1EE3ng, nhi\u1EC7t \u0111\u1ED9 v\xE0 s\u1EF1 chuy\u1EC3n th\u1EC3."],
          ["specific-heat", "SpecificHeatSimulation", "Nhi\u1EC7t dung ri\xEAng", "thermal", "\u0110un n\xF3ng v\u1EADt v\xE0 theo d\xF5i nhi\u1EC7t \u0111\u1ED9."],
          ["phase-change", "PhaseChangeSimulation", "S\u1EF1 chuy\u1EC3n th\u1EC3", "thermal", "Quan s\xE1t c\xE1c qu\xE1 tr\xECnh chuy\u1EC3n th\u1EC3."],
          ["vaporization", "VaporizationSimulation", "S\u1EF1 h\xF3a h\u01A1i", "thermal", "Kh\u1EA3o s\xE1t qu\xE1 tr\xECnh h\xF3a h\u01A1i."],
          ["nucleus", "NucleusStructureSimulation", "C\u1EA5u t\u1EA1o h\u1EA1t nh\xE2n", "nuclear", "Proton, neutron v\xE0 m\xF4 h\xECnh h\u1EA1t nh\xE2n.", 1, true],
          ["nuclear-reaction", "NuclearReactionSimulation", "Ph\u1EA3n \u1EE9ng h\u1EA1t nh\xE2n", "nuclear", "Kh\u1EA3o s\xE1t c\xE1c d\u1EA1ng ph\u1EA3n \u1EE9ng h\u1EA1t nh\xE2n.", 0, true],
          ["radioactive-decay", "RadioactiveDecaySimulation", "Ph\xF3ng x\u1EA1 & chu k\xEC b\xE1n r\xE3", "nuclear", "S\u1ED1 h\u1EA1t c\xF2n l\u1EA1i v\xE0 quy lu\u1EADt ph\xE2n r\xE3."],
          ["newton-inertia-3d", "standalone/co-hoc/dinh-luat-1-newton-quan-tinh-3d.html", "\u0110\u1ECBnh lu\u1EADt I Newton & qu\xE1n t\xEDnh (3D)", "mechanics", "Kh\u1EA3o s\xE1t chuy\u1EC3n \u0111\u1ED9ng th\u1EB3ng \u0111\u1EC1u c\u1EE7a v\u1EADt khi kh\xF4ng ch\u1ECBu t\xE1c d\u1EE5ng c\u1EE7a ngo\u1EA1i l\u1EF1c.", 1, true, "simulation", true],
          ["kinematics-3d", "standalone/co-hoc/mo-phong-dong-hoc-3d.html", "M\xF4 ph\u1ECFng \u0111\u1ED9ng h\u1ECDc 3D", "mechanics", "Kh\u1EA3o s\xE1t chuy\u1EC3n \u0111\u1ED9ng nhanh d\u1EA7n \u0111\u1EC1u, th\u1EB3ng \u0111\u1EC1u, ch\u1EADm d\u1EA7n \u0111\u1EC1u v\u1EDBi \u0111\u1ED3 th\u1ECB d\u2013t, v\u2013t, a\u2013t.", 1, true, "simulation", true],
          ["transformer-3d", "standalone/dien-tu-hoc/may-bien-ap-3d.html", "M\xE1y bi\u1EBFn \xE1p (3D)", "magnetic", "M\xF4 ph\u1ECFng 3D c\u1EA5u t\u1EA1o l\xF5i th\xE9p, cu\u1ED9n s\u01A1 c\u1EA5p, th\u1EE9 c\u1EA5p v\xE0 nguy\xEAn l\xED c\u1EA3m \u1EE9ng \u0111i\u1EC7n t\u1EEB.", 1, true, "simulation", true],
          ["induction-cooker-3d", "standalone/dien-tu-hoc/bep-tu-cam-ung-3d.html", "B\u1EBFp t\u1EEB (c\u1EA3m \u1EE9ng \u0111i\u1EC7n t\u1EEB 3D)", "magnetic", "C\u1EA5u t\u1EA1o 4 t\u1EA7ng, d\xF2ng cao t\u1EA7n IGBT, t\u1EEB tr\u01B0\u1EDDng bi\u1EBFn thi\xEAn v\xE0 d\xF2ng Fu-c\xF4 l\xE0m n\xF3ng \u0111\xE1y n\u1ED3i.", 1, true, "simulation", true],
          ["brownian-motion-3d", "standalone/nhiet-hoc/chuyen-dong-brown-3d.html", "Chuy\u1EC3n \u0111\u1ED9ng Brown (3D)", "thermal", "Quan s\xE1t chuy\u1EC3n \u0111\u1ED9ng h\u1ED7n lo\u1EA1n kh\xF4ng ng\u1EEBng c\u1EE7a h\u1EA1t kh\xF3i/b\u1EE5i do va ch\u1EA1m ph\xE2n t\u1EED kh\xED.", 1, true, "simulation", true],
          ["boyle-law-3d", "standalone/nhiet-hoc/dinh-luat-boyle-3d.html", "\u0110\u1ECBnh lu\u1EADt Boyle (3D)", "thermal", "Th\xED nghi\u1EC7m kh\u1EA3o s\xE1t qu\xE1 tr\xECnh \u0111\u1EB3ng nhi\u1EC7t c\u1EE7a kh\u1ED1i kh\xED v\xE0 \u0111\u1ED3 th\u1ECB p\u2013V trong kh\xF4ng gian 3D.", 1, true, "simulation", true],
          ["steam-engine-3d", "standalone/nhiet-hoc/dong-co-hoi-nuoc-3d.html", "\u0110\u1ED9ng c\u01A1 h\u01A1i n\u01B0\u1EDBc (3D)", "thermal", "M\xF4 h\xECnh 3D nguy\xEAn t\u1EAFc ho\u1EA1t \u0111\u1ED9ng \u0111\u1ED9ng c\u01A1 h\u01A1i n\u01B0\u1EDBc: xi lanh, p\xEDt-t\xF4ng v\xE0 van tr\u01B0\u1EE3t.", 1, true, "simulation", true],
          ["rutherford-alpha-3d", "standalone/vat-li-hat-nhan/tan-xa-hat-alpha-3d.html", "T\xE1n x\u1EA1 h\u1EA1t \u03B1 (Rutherford 3D)", "nuclear", "Th\xED nghi\u1EC7m t\xE1n x\u1EA1 h\u1EA1t alpha qua l\xE1 v\xE0ng m\u1ECFng c\u1EE7a Rutherford v\xE0 m\xF4 h\xECnh nguy\xEAn t\u1EED.", 1, true, "simulation", true],
          ["mass-defect-he4-3d", "standalone/vat-li-hat-nhan/do-hut-khoi-he4-3d.html", "\u0110\u1ED9 h\u1EE5t kh\u1ED1i & li\xEAn k\u1EBFt He-4 (3D)", "nuclear", "Kh\u1ED1i l\u01B0\u1EE3ng h\u1EA1t nh\xE2n Heli-4, \u0111\u1ED9 h\u1EE5t kh\u1ED1i v\xE0 n\u0103ng l\u01B0\u1EE3ng li\xEAn k\u1EBFt t\u1EA1o th\xE0nh h\u1EA1t nh\xE2n.", 1, true, "simulation", true],
          ["nuclear-power-plant-3d", "standalone/vat-li-hat-nhan/nha-may-dien-hat-nhan-3d.html", "Nh\xE0 m\xE1y \u0111i\u1EC7n h\u1EA1t nh\xE2n (3D)", "nuclear", "M\xF4 h\xECnh 3D l\xF2 ph\u1EA3n \u1EE9ng h\u1EA1t nh\xE2n, thanh \u0111i\u1EC1u khi\u1EC3n, b\u1ED9 trao \u0111\u1ED5i nhi\u1EC7t v\xE0 tua-bin.", 1, true, "simulation", true]
        ];
        const items = rows.map(([id, source, title, group, description, three, illustrative, kind, standalone]) => ({ id, source, title, group, description, three: !!three, illustrative: !!illustrative, kind: kind || "simulation", standalone: !!standalone }));
        function data(value, depth = 0, budget = { n: 0 }) {
          if (++budget.n > 18e3 || depth > 7)
            return void 0;
          if (value === null || typeof value === "boolean")
            return value;
          if (typeof value === "number")
            return Number.isFinite(value) && Math.abs(value) <= 1e7 ? value : void 0;
          if (typeof value === "string")
            return value.length <= 2e3 ? value : void 0;
          if (Array.isArray(value)) {
            if (value.length > 2500)
              return void 0;
            const out = value.map((v) => data(v, depth + 1, budget));
            return out.some((v) => v === void 0) ? void 0 : out;
          }
          if (value && typeof value === "object" && Object.getPrototypeOf(value) === Object.prototype) {
            const keys = Object.keys(value);
            if (keys.length > 100)
              return void 0;
            const out = {};
            for (const k of keys) {
              if (!/^[\w.:-]{1,160}$/.test(k) || ["__proto__", "prototype", "constructor"].includes(k))
                return void 0;
              const v = data(value[k], depth + 1, budget);
              if (v === void 0)
                return void 0;
              out[k] = v;
            }
            return out;
          }
          return void 0;
        }
        function clean(input) {
          if (input?.id === "oscillation")
            input = { id: "two-oscillations", states: {}, refs: {} };
          if (!input || typeof input !== "object" || !items.some((i) => i.id === input.id))
            return null;
          const states2 = data(input.states || {}), refs2 = data(input.refs || {});
          if (!states2 || typeof states2 !== "object" || Array.isArray(states2) || !refs2 || typeof refs2 !== "object" || Array.isArray(refs2))
            return null;
          const out = { version: 1, id: input.id, states: states2, refs: refs2 };
          return out;
        }
        function csvCell(value) {
          let s = String(value);
          if (typeof value === "string" && /^[=+\-@\t\r]/.test(s))
            s = "'" + s;
          return '"' + s.replace(/"/g, '""') + '"';
        }
        return { version: 1, groups, items, get: (id) => items.find((i) => i.id === id), clean, data, csvCell };
      });
    }
  });

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react/cjs/react-jsx-runtime.production.min.js
  var require_react_jsx_runtime_production_min = __commonJS({
    "../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react/cjs/react-jsx-runtime.production.min.js"(exports) {
      "use strict";
      var f = require_react();
      var k = Symbol.for("react.element");
      var l = Symbol.for("react.fragment");
      var m = Object.prototype.hasOwnProperty;
      var n = f.__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED.ReactCurrentOwner;
      var p = { key: true, ref: true, __self: true, __source: true };
      function q(c, a, g) {
        var b, d = {}, e = null, h = null;
        void 0 !== g && (e = "" + g);
        void 0 !== a.key && (e = "" + a.key);
        void 0 !== a.ref && (h = a.ref);
        for (b in a)
          m.call(a, b) && !p.hasOwnProperty(b) && (d[b] = a[b]);
        if (c && c.defaultProps)
          for (b in a = c.defaultProps, a)
            void 0 === d[b] && (d[b] = a[b]);
        return { $$typeof: k, type: c, key: e, ref: h, props: d, _owner: n.current };
      }
      exports.Fragment = l;
      exports.jsx = q;
      exports.jsxs = q;
    }
  });

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react/jsx-runtime.js
  var require_jsx_runtime = __commonJS({
    "../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/react/jsx-runtime.js"(exports, module) {
      "use strict";
      if (true) {
        module.exports = require_react_jsx_runtime_production_min();
      } else {
        module.exports = null;
      }
    }
  });

  // physics/src/player.tsx
  var import_react2 = __toESM(require_react());
  var import_client = __toESM(require_client());

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/util.js
  function resolveUrl(url, baseUrl) {
    if (url.match(/^[a-z]+:\/\//i)) {
      return url;
    }
    if (url.match(/^\/\//)) {
      return window.location.protocol + url;
    }
    if (url.match(/^[a-z]+:/i)) {
      return url;
    }
    const doc = document.implementation.createHTMLDocument();
    const base = doc.createElement("base");
    const a = doc.createElement("a");
    doc.head.appendChild(base);
    doc.body.appendChild(a);
    if (baseUrl) {
      base.href = baseUrl;
    }
    a.href = url;
    return a.href;
  }
  var uuid = (() => {
    let counter = 0;
    const random = () => (
      // eslint-disable-next-line no-bitwise
      `0000${(Math.random() * 36 ** 4 << 0).toString(36)}`.slice(-4)
    );
    return () => {
      counter += 1;
      return `u${random()}${counter}`;
    };
  })();
  function toArray(arrayLike) {
    const arr = [];
    for (let i = 0, l = arrayLike.length; i < l; i++) {
      arr.push(arrayLike[i]);
    }
    return arr;
  }
  var styleProps = null;
  function getStyleProperties(options = {}) {
    if (styleProps) {
      return styleProps;
    }
    if (options.includeStyleProperties) {
      styleProps = options.includeStyleProperties;
      return styleProps;
    }
    styleProps = toArray(window.getComputedStyle(document.documentElement));
    return styleProps;
  }
  function px(node, styleProperty) {
    const win = node.ownerDocument.defaultView || window;
    const val = win.getComputedStyle(node).getPropertyValue(styleProperty);
    return val ? parseFloat(val.replace("px", "")) : 0;
  }
  function getNodeWidth(node) {
    const leftBorder = px(node, "border-left-width");
    const rightBorder = px(node, "border-right-width");
    return node.clientWidth + leftBorder + rightBorder;
  }
  function getNodeHeight(node) {
    const topBorder = px(node, "border-top-width");
    const bottomBorder = px(node, "border-bottom-width");
    return node.clientHeight + topBorder + bottomBorder;
  }
  function getImageSize(targetNode, options = {}) {
    const width = options.width || getNodeWidth(targetNode);
    const height = options.height || getNodeHeight(targetNode);
    return { width, height };
  }
  function getPixelRatio() {
    let ratio;
    let FINAL_PROCESS;
    try {
      FINAL_PROCESS = process;
    } catch (e) {
    }
    const val = FINAL_PROCESS && FINAL_PROCESS.env ? FINAL_PROCESS.env.devicePixelRatio : null;
    if (val) {
      ratio = parseInt(val, 10);
      if (Number.isNaN(ratio)) {
        ratio = 1;
      }
    }
    return ratio || window.devicePixelRatio || 1;
  }
  var canvasDimensionLimit = 16384;
  function checkCanvasDimensions(canvas) {
    if (canvas.width > canvasDimensionLimit || canvas.height > canvasDimensionLimit) {
      if (canvas.width > canvasDimensionLimit && canvas.height > canvasDimensionLimit) {
        if (canvas.width > canvas.height) {
          canvas.height *= canvasDimensionLimit / canvas.width;
          canvas.width = canvasDimensionLimit;
        } else {
          canvas.width *= canvasDimensionLimit / canvas.height;
          canvas.height = canvasDimensionLimit;
        }
      } else if (canvas.width > canvasDimensionLimit) {
        canvas.height *= canvasDimensionLimit / canvas.width;
        canvas.width = canvasDimensionLimit;
      } else {
        canvas.width *= canvasDimensionLimit / canvas.height;
        canvas.height = canvasDimensionLimit;
      }
    }
  }
  function createImage(url) {
    return new Promise((resolve, reject) => {
      const img = new Image();
      img.onload = () => {
        img.decode().then(() => {
          requestAnimationFrame(() => resolve(img));
        });
      };
      img.onerror = reject;
      img.crossOrigin = "anonymous";
      img.decoding = "async";
      img.src = url;
    });
  }
  async function svgToDataURL(svg) {
    return Promise.resolve().then(() => new XMLSerializer().serializeToString(svg)).then(encodeURIComponent).then((html) => `data:image/svg+xml;charset=utf-8,${html}`);
  }
  async function nodeToDataURL(node, width, height) {
    const xmlns = "http://www.w3.org/2000/svg";
    const svg = document.createElementNS(xmlns, "svg");
    const foreignObject = document.createElementNS(xmlns, "foreignObject");
    svg.setAttribute("width", `${width}`);
    svg.setAttribute("height", `${height}`);
    svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
    foreignObject.setAttribute("width", "100%");
    foreignObject.setAttribute("height", "100%");
    foreignObject.setAttribute("x", "0");
    foreignObject.setAttribute("y", "0");
    foreignObject.setAttribute("externalResourcesRequired", "true");
    svg.appendChild(foreignObject);
    foreignObject.appendChild(node);
    return svgToDataURL(svg);
  }
  var isInstanceOfElement = (node, instance) => {
    if (node instanceof instance)
      return true;
    const nodePrototype = Object.getPrototypeOf(node);
    if (nodePrototype === null)
      return false;
    return nodePrototype.constructor.name === instance.name || isInstanceOfElement(nodePrototype, instance);
  };

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/clone-pseudos.js
  function formatCSSText(style) {
    const content = style.getPropertyValue("content");
    return `${style.cssText} content: '${content.replace(/'|"/g, "")}';`;
  }
  function formatCSSProperties(style, options) {
    return getStyleProperties(options).map((name) => {
      const value = style.getPropertyValue(name);
      const priority = style.getPropertyPriority(name);
      return `${name}: ${value}${priority ? " !important" : ""};`;
    }).join(" ");
  }
  function getPseudoElementStyle(className, pseudo, style, options) {
    const selector = `.${className}:${pseudo}`;
    const cssText = style.cssText ? formatCSSText(style) : formatCSSProperties(style, options);
    return document.createTextNode(`${selector}{${cssText}}`);
  }
  function clonePseudoElement(nativeNode, clonedNode, pseudo, options) {
    const style = window.getComputedStyle(nativeNode, pseudo);
    const content = style.getPropertyValue("content");
    if (content === "" || content === "none") {
      return;
    }
    const className = uuid();
    try {
      clonedNode.className = `${clonedNode.className} ${className}`;
    } catch (err) {
      return;
    }
    const styleElement = document.createElement("style");
    styleElement.appendChild(getPseudoElementStyle(className, pseudo, style, options));
    clonedNode.appendChild(styleElement);
  }
  function clonePseudoElements(nativeNode, clonedNode, options) {
    clonePseudoElement(nativeNode, clonedNode, ":before", options);
    clonePseudoElement(nativeNode, clonedNode, ":after", options);
  }

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/mimes.js
  var WOFF = "application/font-woff";
  var JPEG = "image/jpeg";
  var mimes = {
    woff: WOFF,
    woff2: WOFF,
    ttf: "application/font-truetype",
    eot: "application/vnd.ms-fontobject",
    png: "image/png",
    jpg: JPEG,
    jpeg: JPEG,
    gif: "image/gif",
    tiff: "image/tiff",
    svg: "image/svg+xml",
    webp: "image/webp"
  };
  function getExtension(url) {
    const match = /\.([^./]*?)$/g.exec(url);
    return match ? match[1] : "";
  }
  function getMimeType(url) {
    const extension = getExtension(url).toLowerCase();
    return mimes[extension] || "";
  }

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/dataurl.js
  function getContentFromDataUrl(dataURL) {
    return dataURL.split(/,/)[1];
  }
  function isDataUrl(url) {
    return url.search(/^(data:)/) !== -1;
  }
  function makeDataUrl(content, mimeType) {
    return `data:${mimeType};base64,${content}`;
  }
  async function fetchAsDataURL(url, init, process2) {
    const res = await fetch(url, init);
    if (res.status === 404) {
      throw new Error(`Resource "${res.url}" not found`);
    }
    const blob = await res.blob();
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onerror = reject;
      reader.onloadend = () => {
        try {
          resolve(process2({ res, result: reader.result }));
        } catch (error) {
          reject(error);
        }
      };
      reader.readAsDataURL(blob);
    });
  }
  var cache = {};
  function getCacheKey(url, contentType, includeQueryParams) {
    let key = url.replace(/\?.*/, "");
    if (includeQueryParams) {
      key = url;
    }
    if (/ttf|otf|eot|woff2?/i.test(key)) {
      key = key.replace(/.*\//, "");
    }
    return contentType ? `[${contentType}]${key}` : key;
  }
  async function resourceToDataURL(resourceUrl, contentType, options) {
    const cacheKey = getCacheKey(resourceUrl, contentType, options.includeQueryParams);
    if (cache[cacheKey] != null) {
      return cache[cacheKey];
    }
    if (options.cacheBust) {
      resourceUrl += (/\?/.test(resourceUrl) ? "&" : "?") + (/* @__PURE__ */ new Date()).getTime();
    }
    let dataURL;
    try {
      const content = await fetchAsDataURL(resourceUrl, options.fetchRequestInit, ({ res, result }) => {
        if (!contentType) {
          contentType = res.headers.get("Content-Type") || "";
        }
        return getContentFromDataUrl(result);
      });
      dataURL = makeDataUrl(content, contentType);
    } catch (error) {
      dataURL = options.imagePlaceholder || "";
      let msg = `Failed to fetch resource: ${resourceUrl}`;
      if (error) {
        msg = typeof error === "string" ? error : error.message;
      }
      if (msg) {
        console.warn(msg);
      }
    }
    cache[cacheKey] = dataURL;
    return dataURL;
  }

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/clone-node.js
  async function cloneCanvasElement(canvas) {
    const dataURL = canvas.toDataURL();
    if (dataURL === "data:,") {
      return canvas.cloneNode(false);
    }
    return createImage(dataURL);
  }
  async function cloneVideoElement(video, options) {
    if (video.currentSrc) {
      const canvas = document.createElement("canvas");
      const ctx = canvas.getContext("2d");
      canvas.width = video.clientWidth;
      canvas.height = video.clientHeight;
      ctx === null || ctx === void 0 ? void 0 : ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
      const dataURL2 = canvas.toDataURL();
      return createImage(dataURL2);
    }
    const poster = video.poster;
    const contentType = getMimeType(poster);
    const dataURL = await resourceToDataURL(poster, contentType, options);
    return createImage(dataURL);
  }
  async function cloneIFrameElement(iframe, options) {
    var _a;
    try {
      if ((_a = iframe === null || iframe === void 0 ? void 0 : iframe.contentDocument) === null || _a === void 0 ? void 0 : _a.body) {
        return await cloneNode(iframe.contentDocument.body, options, true);
      }
    } catch (_b) {
    }
    return iframe.cloneNode(false);
  }
  async function cloneSingleNode(node, options) {
    if (isInstanceOfElement(node, HTMLCanvasElement)) {
      return cloneCanvasElement(node);
    }
    if (isInstanceOfElement(node, HTMLVideoElement)) {
      return cloneVideoElement(node, options);
    }
    if (isInstanceOfElement(node, HTMLIFrameElement)) {
      return cloneIFrameElement(node, options);
    }
    return node.cloneNode(isSVGElement(node));
  }
  var isSlotElement = (node) => node.tagName != null && node.tagName.toUpperCase() === "SLOT";
  var isSVGElement = (node) => node.tagName != null && node.tagName.toUpperCase() === "SVG";
  async function cloneChildren(nativeNode, clonedNode, options) {
    var _a, _b;
    if (isSVGElement(clonedNode)) {
      return clonedNode;
    }
    let children = [];
    if (isSlotElement(nativeNode) && nativeNode.assignedNodes) {
      children = toArray(nativeNode.assignedNodes());
    } else if (isInstanceOfElement(nativeNode, HTMLIFrameElement) && ((_a = nativeNode.contentDocument) === null || _a === void 0 ? void 0 : _a.body)) {
      children = toArray(nativeNode.contentDocument.body.childNodes);
    } else {
      children = toArray(((_b = nativeNode.shadowRoot) !== null && _b !== void 0 ? _b : nativeNode).childNodes);
    }
    if (children.length === 0 || isInstanceOfElement(nativeNode, HTMLVideoElement)) {
      return clonedNode;
    }
    await children.reduce((deferred, child) => deferred.then(() => cloneNode(child, options)).then((clonedChild) => {
      if (clonedChild) {
        clonedNode.appendChild(clonedChild);
      }
    }), Promise.resolve());
    return clonedNode;
  }
  function cloneCSSStyle(nativeNode, clonedNode, options) {
    const targetStyle = clonedNode.style;
    if (!targetStyle) {
      return;
    }
    const sourceStyle = window.getComputedStyle(nativeNode);
    if (sourceStyle.cssText) {
      targetStyle.cssText = sourceStyle.cssText;
      targetStyle.transformOrigin = sourceStyle.transformOrigin;
    } else {
      getStyleProperties(options).forEach((name) => {
        let value = sourceStyle.getPropertyValue(name);
        if (name === "font-size" && value.endsWith("px")) {
          const reducedFont = Math.floor(parseFloat(value.substring(0, value.length - 2))) - 0.1;
          value = `${reducedFont}px`;
        }
        if (isInstanceOfElement(nativeNode, HTMLIFrameElement) && name === "display" && value === "inline") {
          value = "block";
        }
        if (name === "d" && clonedNode.getAttribute("d")) {
          value = `path(${clonedNode.getAttribute("d")})`;
        }
        targetStyle.setProperty(name, value, sourceStyle.getPropertyPriority(name));
      });
    }
  }
  function cloneInputValue(nativeNode, clonedNode) {
    if (isInstanceOfElement(nativeNode, HTMLTextAreaElement)) {
      clonedNode.innerHTML = nativeNode.value;
    }
    if (isInstanceOfElement(nativeNode, HTMLInputElement)) {
      clonedNode.setAttribute("value", nativeNode.value);
    }
  }
  function cloneSelectValue(nativeNode, clonedNode) {
    if (isInstanceOfElement(nativeNode, HTMLSelectElement)) {
      const clonedSelect = clonedNode;
      const selectedOption = Array.from(clonedSelect.children).find((child) => nativeNode.value === child.getAttribute("value"));
      if (selectedOption) {
        selectedOption.setAttribute("selected", "");
      }
    }
  }
  function decorate(nativeNode, clonedNode, options) {
    if (isInstanceOfElement(clonedNode, Element)) {
      cloneCSSStyle(nativeNode, clonedNode, options);
      clonePseudoElements(nativeNode, clonedNode, options);
      cloneInputValue(nativeNode, clonedNode);
      cloneSelectValue(nativeNode, clonedNode);
    }
    return clonedNode;
  }
  async function ensureSVGSymbols(clone, options) {
    const uses = clone.querySelectorAll ? clone.querySelectorAll("use") : [];
    if (uses.length === 0) {
      return clone;
    }
    const processedDefs = {};
    for (let i = 0; i < uses.length; i++) {
      const use = uses[i];
      const id = use.getAttribute("xlink:href");
      if (id) {
        const exist = clone.querySelector(id);
        const definition = document.querySelector(id);
        if (!exist && definition && !processedDefs[id]) {
          processedDefs[id] = await cloneNode(definition, options, true);
        }
      }
    }
    const nodes = Object.values(processedDefs);
    if (nodes.length) {
      const ns = "http://www.w3.org/1999/xhtml";
      const svg = document.createElementNS(ns, "svg");
      svg.setAttribute("xmlns", ns);
      svg.style.position = "absolute";
      svg.style.width = "0";
      svg.style.height = "0";
      svg.style.overflow = "hidden";
      svg.style.display = "none";
      const defs = document.createElementNS(ns, "defs");
      svg.appendChild(defs);
      for (let i = 0; i < nodes.length; i++) {
        defs.appendChild(nodes[i]);
      }
      clone.appendChild(svg);
    }
    return clone;
  }
  async function cloneNode(node, options, isRoot) {
    if (!isRoot && options.filter && !options.filter(node)) {
      return null;
    }
    return Promise.resolve(node).then((clonedNode) => cloneSingleNode(clonedNode, options)).then((clonedNode) => cloneChildren(node, clonedNode, options)).then((clonedNode) => decorate(node, clonedNode, options)).then((clonedNode) => ensureSVGSymbols(clonedNode, options));
  }

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/embed-resources.js
  var URL_REGEX = /url\((['"]?)([^'"]+?)\1\)/g;
  var URL_WITH_FORMAT_REGEX = /url\([^)]+\)\s*format\((["']?)([^"']+)\1\)/g;
  var FONT_SRC_REGEX = /src:\s*(?:url\([^)]+\)\s*format\([^)]+\)[,;]\s*)+/g;
  function toRegex(url) {
    const escaped = url.replace(/([.*+?^${}()|\[\]\/\\])/g, "\\$1");
    return new RegExp(`(url\\(['"]?)(${escaped})(['"]?\\))`, "g");
  }
  function parseURLs(cssText) {
    const urls = [];
    cssText.replace(URL_REGEX, (raw, quotation, url) => {
      urls.push(url);
      return raw;
    });
    return urls.filter((url) => !isDataUrl(url));
  }
  async function embed(cssText, resourceURL, baseURL, options, getContentFromUrl) {
    try {
      const resolvedURL = baseURL ? resolveUrl(resourceURL, baseURL) : resourceURL;
      const contentType = getMimeType(resourceURL);
      let dataURL;
      if (getContentFromUrl) {
        const content = await getContentFromUrl(resolvedURL);
        dataURL = makeDataUrl(content, contentType);
      } else {
        dataURL = await resourceToDataURL(resolvedURL, contentType, options);
      }
      return cssText.replace(toRegex(resourceURL), `$1${dataURL}$3`);
    } catch (error) {
    }
    return cssText;
  }
  function filterPreferredFontFormat(str, { preferredFontFormat }) {
    return !preferredFontFormat ? str : str.replace(FONT_SRC_REGEX, (match) => {
      while (true) {
        const [src, , format] = URL_WITH_FORMAT_REGEX.exec(match) || [];
        if (!format) {
          return "";
        }
        if (format === preferredFontFormat) {
          return `src: ${src};`;
        }
      }
    });
  }
  function shouldEmbed(url) {
    return url.search(URL_REGEX) !== -1;
  }
  async function embedResources(cssText, baseUrl, options) {
    if (!shouldEmbed(cssText)) {
      return cssText;
    }
    const filteredCSSText = filterPreferredFontFormat(cssText, options);
    const urls = parseURLs(filteredCSSText);
    return urls.reduce((deferred, url) => deferred.then((css) => embed(css, url, baseUrl, options)), Promise.resolve(filteredCSSText));
  }

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/embed-images.js
  async function embedProp(propName, node, options) {
    var _a;
    const propValue = (_a = node.style) === null || _a === void 0 ? void 0 : _a.getPropertyValue(propName);
    if (propValue) {
      const cssString = await embedResources(propValue, null, options);
      node.style.setProperty(propName, cssString, node.style.getPropertyPriority(propName));
      return true;
    }
    return false;
  }
  async function embedBackground(clonedNode, options) {
    ;
    await embedProp("background", clonedNode, options) || await embedProp("background-image", clonedNode, options);
    await embedProp("mask", clonedNode, options) || await embedProp("-webkit-mask", clonedNode, options) || await embedProp("mask-image", clonedNode, options) || await embedProp("-webkit-mask-image", clonedNode, options);
  }
  async function embedImageNode(clonedNode, options) {
    const isImageElement = isInstanceOfElement(clonedNode, HTMLImageElement);
    if (!(isImageElement && !isDataUrl(clonedNode.src)) && !(isInstanceOfElement(clonedNode, SVGImageElement) && !isDataUrl(clonedNode.href.baseVal))) {
      return;
    }
    const url = isImageElement ? clonedNode.src : clonedNode.href.baseVal;
    const dataURL = await resourceToDataURL(url, getMimeType(url), options);
    await new Promise((resolve, reject) => {
      clonedNode.onload = resolve;
      clonedNode.onerror = options.onImageErrorHandler ? (...attributes) => {
        try {
          resolve(options.onImageErrorHandler(...attributes));
        } catch (error) {
          reject(error);
        }
      } : reject;
      const image = clonedNode;
      if (image.decode) {
        image.decode = resolve;
      }
      if (image.loading === "lazy") {
        image.loading = "eager";
      }
      if (isImageElement) {
        clonedNode.srcset = "";
        clonedNode.src = dataURL;
      } else {
        clonedNode.href.baseVal = dataURL;
      }
    });
  }
  async function embedChildren(clonedNode, options) {
    const children = toArray(clonedNode.childNodes);
    const deferreds = children.map((child) => embedImages(child, options));
    await Promise.all(deferreds).then(() => clonedNode);
  }
  async function embedImages(clonedNode, options) {
    if (isInstanceOfElement(clonedNode, Element)) {
      await embedBackground(clonedNode, options);
      await embedImageNode(clonedNode, options);
      await embedChildren(clonedNode, options);
    }
  }

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/apply-style.js
  function applyStyle(node, options) {
    const { style } = node;
    if (options.backgroundColor) {
      style.backgroundColor = options.backgroundColor;
    }
    if (options.width) {
      style.width = `${options.width}px`;
    }
    if (options.height) {
      style.height = `${options.height}px`;
    }
    const manual = options.style;
    if (manual != null) {
      Object.keys(manual).forEach((key) => {
        style[key] = manual[key];
      });
    }
    return node;
  }

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/embed-webfonts.js
  var cssFetchCache = {};
  async function fetchCSS(url) {
    let cache2 = cssFetchCache[url];
    if (cache2 != null) {
      return cache2;
    }
    const res = await fetch(url);
    const cssText = await res.text();
    cache2 = { url, cssText };
    cssFetchCache[url] = cache2;
    return cache2;
  }
  async function embedFonts(data, options) {
    let cssText = data.cssText;
    const regexUrl = /url\(["']?([^"')]+)["']?\)/g;
    const fontLocs = cssText.match(/url\([^)]+\)/g) || [];
    const loadFonts = fontLocs.map(async (loc) => {
      let url = loc.replace(regexUrl, "$1");
      if (!url.startsWith("https://")) {
        url = new URL(url, data.url).href;
      }
      return fetchAsDataURL(url, options.fetchRequestInit, ({ result }) => {
        cssText = cssText.replace(loc, `url(${result})`);
        return [loc, result];
      });
    });
    return Promise.all(loadFonts).then(() => cssText);
  }
  function parseCSS(source) {
    if (source == null) {
      return [];
    }
    const result = [];
    const commentsRegex = /(\/\*[\s\S]*?\*\/)/gi;
    let cssText = source.replace(commentsRegex, "");
    const keyframesRegex = new RegExp("((@.*?keyframes [\\s\\S]*?){([\\s\\S]*?}\\s*?)})", "gi");
    while (true) {
      const matches = keyframesRegex.exec(cssText);
      if (matches === null) {
        break;
      }
      result.push(matches[0]);
    }
    cssText = cssText.replace(keyframesRegex, "");
    const importRegex = /@import[\s\S]*?url\([^)]*\)[\s\S]*?;/gi;
    const combinedCSSRegex = "((\\s*?(?:\\/\\*[\\s\\S]*?\\*\\/)?\\s*?@media[\\s\\S]*?){([\\s\\S]*?)}\\s*?})|(([\\s\\S]*?){([\\s\\S]*?)})";
    const unifiedRegex = new RegExp(combinedCSSRegex, "gi");
    while (true) {
      let matches = importRegex.exec(cssText);
      if (matches === null) {
        matches = unifiedRegex.exec(cssText);
        if (matches === null) {
          break;
        } else {
          importRegex.lastIndex = unifiedRegex.lastIndex;
        }
      } else {
        unifiedRegex.lastIndex = importRegex.lastIndex;
      }
      result.push(matches[0]);
    }
    return result;
  }
  async function getCSSRules(styleSheets, options) {
    const ret = [];
    const deferreds = [];
    styleSheets.forEach((sheet) => {
      if ("cssRules" in sheet) {
        try {
          toArray(sheet.cssRules || []).forEach((item, index) => {
            if (item.type === CSSRule.IMPORT_RULE) {
              let importIndex = index + 1;
              const url = item.href;
              const deferred = fetchCSS(url).then((metadata) => embedFonts(metadata, options)).then((cssText) => parseCSS(cssText).forEach((rule) => {
                try {
                  sheet.insertRule(rule, rule.startsWith("@import") ? importIndex += 1 : sheet.cssRules.length);
                } catch (error) {
                  console.error("Error inserting rule from remote css", {
                    rule,
                    error
                  });
                }
              })).catch((e) => {
                console.error("Error loading remote css", e.toString());
              });
              deferreds.push(deferred);
            }
          });
        } catch (e) {
          const inline = styleSheets.find((a) => a.href == null) || document.styleSheets[0];
          if (sheet.href != null) {
            deferreds.push(fetchCSS(sheet.href).then((metadata) => embedFonts(metadata, options)).then((cssText) => parseCSS(cssText).forEach((rule) => {
              inline.insertRule(rule, inline.cssRules.length);
            })).catch((err) => {
              console.error("Error loading remote stylesheet", err);
            }));
          }
          console.error("Error inlining remote css file", e);
        }
      }
    });
    return Promise.all(deferreds).then(() => {
      styleSheets.forEach((sheet) => {
        if ("cssRules" in sheet) {
          try {
            toArray(sheet.cssRules || []).forEach((item) => {
              ret.push(item);
            });
          } catch (e) {
            console.error(`Error while reading CSS rules from ${sheet.href}`, e);
          }
        }
      });
      return ret;
    });
  }
  function getWebFontRules(cssRules) {
    return cssRules.filter((rule) => rule.type === CSSRule.FONT_FACE_RULE).filter((rule) => shouldEmbed(rule.style.getPropertyValue("src")));
  }
  async function parseWebFontRules(node, options) {
    if (node.ownerDocument == null) {
      throw new Error("Provided element is not within a Document");
    }
    const styleSheets = toArray(node.ownerDocument.styleSheets);
    const cssRules = await getCSSRules(styleSheets, options);
    return getWebFontRules(cssRules);
  }
  function normalizeFontFamily(font) {
    return font.trim().replace(/["']/g, "");
  }
  function getUsedFonts(node) {
    const fonts = /* @__PURE__ */ new Set();
    function traverse(node2) {
      const fontFamily = node2.style.fontFamily || getComputedStyle(node2).fontFamily;
      fontFamily.split(",").forEach((font) => {
        fonts.add(normalizeFontFamily(font));
      });
      Array.from(node2.children).forEach((child) => {
        if (child instanceof HTMLElement) {
          traverse(child);
        }
      });
    }
    traverse(node);
    return fonts;
  }
  async function getWebFontCSS(node, options) {
    const rules = await parseWebFontRules(node, options);
    const usedFonts = getUsedFonts(node);
    const cssTexts = await Promise.all(rules.filter((rule) => usedFonts.has(normalizeFontFamily(rule.style.fontFamily))).map((rule) => {
      const baseUrl = rule.parentStyleSheet ? rule.parentStyleSheet.href : null;
      return embedResources(rule.cssText, baseUrl, options);
    }));
    return cssTexts.join("\n");
  }
  async function embedWebFonts(clonedNode, options) {
    const cssText = options.fontEmbedCSS != null ? options.fontEmbedCSS : options.skipFonts ? null : await getWebFontCSS(clonedNode, options);
    if (cssText) {
      const styleNode = document.createElement("style");
      const sytleContent = document.createTextNode(cssText);
      styleNode.appendChild(sytleContent);
      if (clonedNode.firstChild) {
        clonedNode.insertBefore(styleNode, clonedNode.firstChild);
      } else {
        clonedNode.appendChild(styleNode);
      }
    }
  }

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/html-to-image/es/index.js
  async function toSvg(node, options = {}) {
    const { width, height } = getImageSize(node, options);
    const clonedNode = await cloneNode(node, options, true);
    await embedWebFonts(clonedNode, options);
    await embedImages(clonedNode, options);
    applyStyle(clonedNode, options);
    const datauri = await nodeToDataURL(clonedNode, width, height);
    return datauri;
  }
  async function toCanvas(node, options = {}) {
    const { width, height } = getImageSize(node, options);
    const svg = await toSvg(node, options);
    const img = await createImage(svg);
    const canvas = document.createElement("canvas");
    const context = canvas.getContext("2d");
    const ratio = options.pixelRatio || getPixelRatio();
    const canvasWidth = options.canvasWidth || width;
    const canvasHeight = options.canvasHeight || height;
    canvas.width = canvasWidth * ratio;
    canvas.height = canvasHeight * ratio;
    if (!options.skipAutoScale) {
      checkCanvasDimensions(canvas);
    }
    canvas.style.width = `${canvasWidth}`;
    canvas.style.height = `${canvasHeight}`;
    if (options.backgroundColor) {
      context.fillStyle = options.backgroundColor;
      context.fillRect(0, 0, canvas.width, canvas.height);
    }
    context.drawImage(img, 0, 0, canvas.width, canvas.height);
    return canvas;
  }
  async function toPng(node, options = {}) {
    const canvas = await toCanvas(node, options);
    return canvas.toDataURL();
  }

  // physics/src/player.tsx
  var import_physics_catalog2 = __toESM(require_physics_catalog());

  // physics/src/bridge.ts
  var import_react = __toESM(require_react());
  var import_physics_catalog = __toESM(require_physics_catalog());

  // physics/schema.json
  var schema_default = {
    "force-vector": {},
    "inclined-plane": {
      "InclinedPlaneSimulation.angle": {
        min: 5,
        max: 55
      },
      "InclinedPlaneSimulation.m": {
        min: 0.5,
        max: 5
      },
      "InclinedPlaneSimulation.mu": {
        min: 0,
        max: 1
      },
      "InclinedPlaneSimulation.v0": {
        min: 0,
        max: 5
      }
    },
    collision: {
      "CollisionSimulation.m1": {
        min: 1,
        max: 6
      },
      "CollisionSimulation.m2": {
        min: 1,
        max: 6
      },
      "CollisionSimulation.v1": {
        min: -100,
        max: 150
      },
      "CollisionSimulation.v2": {
        min: -150,
        max: 100
      }
    },
    projectile: {
      "ProjectileMotionSimulation.v0": {
        min: 6,
        max: 28
      },
      "ProjectileMotionSimulation.angle": {
        min: 10,
        max: 80
      },
      "ProjectileMotionSimulation.h": {
        min: 10,
        max: 60
      }
    },
    "free-fall": {
      "FreeFall3DSimulation.H": {
        min: 0,
        max: 30
      }
    },
    "circular-motion": {
      "CircularMotionSimulation.omega": {
        min: 0.4,
        max: 3
      },
      "CircularMotionSimulation.r": {
        min: 0.4,
        max: 2
      }
    },
    "motion-graphs": {
      "MotionGraphsSimulation.v0": {
        min: 0,
        max: 10
      },
      "MotionGraphsSimulation.a0": {
        min: -4,
        max: 4
      }
    },
    "displacement-time": {},
    "vt-graph": {
      "InteractiveVTGraph.acceleration": {
        min: -5,
        max: 5
      },
      "InteractiveVTGraph.initialVelocity": {
        min: -10,
        max: 15
      }
    },
    "hooke-spring": {
      "HookeSpringSimulation.k": {
        min: 5,
        max: 50
      },
      "HookeSpringSimulation.dl": {
        min: -0.1,
        max: 0.3
      }
    },
    "spring-oscillator": {
      "SpringOscillatorSimulation.k": {
        min: 5,
        max: 50
      },
      "SpringOscillatorSimulation.m": {
        min: 0.5,
        max: 3
      },
      "SpringOscillatorSimulation.mu": {
        min: 0,
        max: 0.3
      },
      "SpringOscillatorSimulation.v0": {
        min: 0,
        max: 3
      }
    },
    "simple-pendulum": {
      "SimplePendulumSimulation.l": {
        min: 0.5,
        max: 2.5
      },
      "SimplePendulumSimulation.c": {
        min: 0,
        max: 2
      },
      "SimplePendulumSimulation.v0": {
        min: 0,
        max: 3
      }
    },
    "energy-oscillation": {
      "EnergyOscillationSimulation.A": {
        min: 0.4,
        max: 1
      },
      "EnergyOscillationSimulation.omega": {
        min: 1,
        max: 4
      }
    },
    "two-oscillations": {
      "TwoOscillationsSimulation.a1": {
        min: 0.2,
        max: 1.4
      },
      "TwoOscillationsSimulation.a2": {
        min: 0.2,
        max: 1.4
      }
    },
    "driven-resonance": {
      "DrivenResonanceSimulation.wf": {
        min: 0.2,
        max: 2
      },
      "DrivenResonanceSimulation.beta": {
        min: 0.04,
        max: 0.6
      }
    },
    "wave-propagation": {
      "WavePropagationSimulation.A": {
        min: 10,
        max: 80
      },
      "WavePropagationSimulation.lambda": {
        min: 80,
        max: 320
      },
      "WavePropagationSimulation.f": {
        min: 0.2,
        max: 2
      }
    },
    "wave-interference": {
      "WaveInterferenceSimulation.dist": {
        min: 50,
        max: 220
      },
      "WaveInterferenceSimulation.lambda": {
        min: 24,
        max: 72
      }
    },
    "standing-wave": {
      "StandingWaveSimulation.n": {
        min: 1,
        max: 6
      }
    },
    coulomb: {
      "CoulombForceSimulation.q1": {
        min: -6,
        max: 6
      },
      "CoulombForceSimulation.q2": {
        min: -6,
        max: 6
      },
      "CoulombForceSimulation.rVal": {
        min: 90,
        max: 460
      }
    },
    "electric-field": {
      "ElectricFieldSimulation.v0": {
        min: 0,
        max: 260
      },
      "ElectricFieldSimulation.angle": {
        min: -90,
        max: 90
      },
      "ElectricFieldSimulation.E": {
        min: 0,
        max: 6
      },
      "ElectricFieldSimulation.q": {
        min: -3,
        max: 3
      }
    },
    "resistance-ohm": {
      "ResistanceOhmSimulation.U": {
        min: 0,
        max: 12
      },
      "ResistanceOhmSimulation.R": {
        min: 2,
        max: 6
      },
      "ResistanceOhmSimulation.T": {
        min: 0,
        max: 200
      }
    },
    circuit: {
      "CircuitSimulation.volt": {
        min: 1,
        max: 9
      }
    },
    "emf-measurement": {
      "EMFMeasurementSimulation.E": {
        min: 1.5,
        max: 9
      },
      "EMFMeasurementSimulation.r": {
        min: 0.2,
        max: 3
      }
    },
    "magnetic-field": {},
    "magnetic-force": {},
    "em-induction": {},
    "ac-generator": {
      "AcGeneratorSimulation.omega": {
        min: 0.5,
        max: 5
      }
    },
    "gas-law": {
      "GasLawSimulation.T": {
        min: 150,
        max: 500
      },
      "GasLawSimulation.p": {
        min: 1,
        max: 6
      }
    },
    "gas-particles": {
      "GasParticleSimulation.temp": {
        min: 1,
        max: 10
      },
      "GasParticleSimulation.count": {
        min: 20,
        max: 150
      }
    },
    "heating-curve": {
      "HeatingCurveSimulation.q": {
        min: 0,
        max: 1
      }
    },
    "specific-heat": {
      "SpecificHeatSimulation.m": {
        min: 0.5,
        max: 5
      }
    },
    "phase-change": {
      "PhaseChangeSimulation.temp": {
        min: 1,
        max: 10
      }
    },
    vaporization: {
      "VaporizationSimulation.q": {
        min: 0,
        max: 1
      }
    },
    nucleus: {},
    "nuclear-reaction": {
      "NuclearReactionSimulation.speed": {
        min: 0.4,
        max: 2.6
      }
    },
    "radioactive-decay": {
      "RadioactiveDecaySimulation.t": {
        min: 0
      },
      "RadioactiveDecaySimulation.halfLife": {
        min: 1,
        max: 15
      },
      "RadioactiveDecaySimulation.n0": {
        min: 50,
        max: 400
      }
    }
  };

  // physics/src/bridge.ts
  var running = (key) => /\.(?:running|playing|isPlaying)$/.test(key);
  var transient = (key) => /\.(?:raf|last\w*|drag\w*|first\w*|hasCompleted\w*|param\w*|runRef|playRef|playingRef|runningRef|\w*Timer|\w*Raf)$/i.test(key);
  var copy = (v) => import_physics_catalog.default.data(v);
  var scene;
  var notify = () => {
  };
  var restoring = false;
  var states = /* @__PURE__ */ new Map();
  var refs = /* @__PURE__ */ new Map();
  var setters = /* @__PURE__ */ new Map();
  var cameras = /* @__PURE__ */ new Map();
  function restoreCamera(key, state) {
    const saved = scene?.refs?.[key];
    if (!saved || typeof saved !== "object")
      return;
    const vector = (v, n) => Array.isArray(v) && v.length === n && v.every((x) => Number.isFinite(x) && Math.abs(x) < 1e6);
    const camera = state.camera, controls = state.get?.().controls;
    if (vector(saved.position, 3))
      camera.position.fromArray(saved.position);
    if (vector(saved.quaternion, 4) && Math.hypot(...saved.quaternion) > 1e-8)
      camera.quaternion.fromArray(saved.quaternion).normalize();
    if (Number.isFinite(saved.zoom))
      camera.zoom = Math.max(0.05, Math.min(100, saved.zoom));
    if (controls?.target && vector(saved.target, 3))
      controls.target.fromArray(saved.target);
    camera.updateProjectionMatrix();
    controls?.update?.();
    state.invalidate?.();
  }
  function seed(key, value, initial) {
    if (value === void 0 || typeof value !== typeof initial || value === null !== (initial === null) || Array.isArray(value) !== Array.isArray(initial))
      return initial;
    const safe = copy(value);
    if (safe === void 0)
      return initial;
    if (initial && typeof initial === "object" && !Array.isArray(initial) && Object.keys(initial).some((k) => !(k in safe) || typeof safe[k] !== typeof initial[k]))
      return initial;
    const spec = schema_default[scene?.id]?.[key];
    if (typeof safe === "number" && spec) {
      let n = safe;
      if (Number.isFinite(spec.min))
        n = Math.max(spec.min, n);
      if (Number.isFinite(spec.max))
        n = Math.min(spec.max, n);
      return n;
    }
    if (spec?.choices && !spec.choices.includes(safe))
      return initial;
    return safe;
  }
  function useSimState(key, initial) {
    const [value, setValue] = (0, import_react.useState)(() => {
      const v = typeof initial === "function" ? initial() : initial;
      return running(key) ? false : seed(key, scene?.states?.[key], v);
    });
    const latest = (0, import_react.useRef)(value);
    latest.current = value;
    states.set(key, () => latest.current);
    setters.set(key, setValue);
    (0, import_react.useEffect)(() => {
      if (!restoring)
        notify();
    }, [value]);
    return [value, setValue];
  }
  function useSimRef(key, initial) {
    const holder = (0, import_react.useRef)(transient(key) ? initial : seed(key, scene?.refs?.[key], initial));
    if (!transient(key))
      refs.set(key, holder);
    return holder;
  }
  var bridge = {
    start(value, onChange) {
      scene = value;
      notify = onChange;
      states.clear();
      refs.clear();
      setters.clear();
      cameras.clear();
    },
    snapshot() {
      const s = {}, r = {};
      for (const [k, get] of states) {
        const v = running(k) ? false : copy(get());
        if (v !== void 0)
          s[k] = v;
      }
      for (const [k, ref] of refs) {
        const v = copy(ref.current);
        if (v !== void 0)
          r[k] = v;
      }
      for (const [k, root] of cameras) {
        const camera = root.camera, target = root.get?.().controls?.target;
        r[k] = { position: camera.position.toArray(), quaternion: camera.quaternion.toArray(), zoom: camera.zoom, ...target ? { target: target.toArray() } : {} };
      }
      return import_physics_catalog.default.clean({ ...scene, states: s, refs: r });
    },
    restoreRefs() {
      restoring = true;
      for (const [k, ref] of refs)
        if (scene?.refs?.[k] !== void 0)
          ref.current = seed(k, scene.refs[k], ref.current);
      for (const [k, root] of cameras)
        restoreCamera(k, root);
      restoring = false;
    },
    pause() {
      for (const [k, set] of setters)
        if (running(k))
          set(false);
    },
    play() {
      for (const [k, set] of setters)
        if (running(k))
          set(true);
    },
    get running() {
      return [...states].some(([k, get]) => running(k) && get() === true);
    },
    get canPlay() {
      return [...states.keys()].some(running);
    },
    get state() {
      return Object.fromEntries([...states].map(([k, get]) => [k, get()]));
    }
  };

  // physics/src/player.tsx
  var import_jsx_runtime = __toESM(require_jsx_runtime());
  function mount(modules) {
    const q = new URLSearchParams(location.hash.slice(1)), boot = window.NBPhysicsBoot, token = boot?.token || q.get("token"), id = boot?.id || q.get("id"), item = import_physics_catalog2.default.get(id);
    const rootEl = document.getElementById("simulation"), root = (0, import_client.createRoot)(rootEl);
    let scene2, ready = false, changed = false, timer, closed = false, capturing = false, lastState = "", lastPlayback = "", restoreTimer, pendingCapture;
    const send = (type, extra = {}) => parent.postMessage({ nbPhysics: 1, token, type, ...extra }, "*");
    const status = document.getElementById("playerStatus");
    let fitRaf = 0;
    function fitVisuals() {
      const visual = rootEl.querySelector(".sim-visuals");
      if (!visual)
        return;
      const canvases = [...visual.querySelectorAll("canvas[data-logical-width]")];
      const width = visual.clientWidth, available = Math.max(160, innerWidth > 740 ? innerHeight - 28 : Math.min(innerHeight * 0.48, 420));
      const stages = [...visual.children].filter((el) => el.querySelector("canvas:not([data-logical-width])"));
      const stageHeight = Math.max(160, Math.min(canvases.length ? available * 0.62 : available, 420));
      for (const el of stages) {
        let stage = el;
        const webgl = el.querySelector("canvas:not([data-logical-width])");
        let p = webgl?.parentElement;
        while (p && p !== visual) {
          if (/px$/.test(p.style.height) && !p.querySelector("canvas[data-logical-width]"))
            stage = p;
          p = p.parentElement;
        }
        stage.style.height = stageHeight + "px";
        stage.style.maxHeight = stageHeight + "px";
      }
      const nested = (cv) => stages.find((el) => el.contains(cv));
      let total = 0;
      for (const cv of canvases)
        if (!nested(cv))
          total += Number(cv.dataset.logicalHeight) / Number(cv.dataset.logicalWidth) * Math.min(width, cv.parentElement?.clientWidth || width);
      const scale = Math.min(1, Math.max(100, available - stages.length * (stageHeight + 8) - Math.max(0, canvases.length - 1) * 8) / Math.max(1, total));
      for (const cv of canvases) {
        const ratio = Number(cv.dataset.logicalHeight) / Number(cv.dataset.logicalWidth), ownWidth = Math.min(width, cv.parentElement?.clientWidth || width), ownScale = nested(cv) ? Math.min(1, Math.max(100, innerWidth > 740 ? available : available - stageHeight - 8) / (ownWidth * ratio)) : scale, w = ownWidth * ownScale;
        cv.style.width = w + "px";
        cv.style.height = w * ratio + "px";
        cv.style.aspectRatio = String(1 / ratio);
      }
    }
    const fit = () => {
      cancelAnimationFrame(fitRaf);
      fitRaf = requestAnimationFrame(fitVisuals);
    };
    const resize = new ResizeObserver(fit);
    resize.observe(document.getElementById("simViewport"));
    addEventListener("resize", fit);
    const mutations = new MutationObserver(fit);
    mutations.observe(rootEl, { childList: true, subtree: true });
    class Boundary extends import_react2.Component {
      constructor() {
        super(...arguments);
        __publicField(this, "state", { error: "" });
      }
      static getDerivedStateFromError() {
        return { error: "Kh\xF4ng m\u1EDF \u0111\u01B0\u1EE3c m\xF4 ph\u1ECFng tr\xEAn thi\u1EBFt b\u1ECB n\xE0y." };
      }
      componentDidCatch(e) {
        send("error", { message: String(e.message).slice(0, 300) });
      }
      render() {
        return this.state.error ? /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { role: "alert", children: [
          /* @__PURE__ */ (0, import_jsx_runtime.jsx)("h2", { children: this.state.error }),
          /* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", { children: "Th\u1EED t\u1EA3i l\u1EA1i ho\u1EB7c ch\u1ECDn m\xF4 ph\u1ECFng 2D." })
        ] }) : this.props.children;
      }
    }
    function announce(force = false) {
      if (!ready || closed)
        return;
      const snapshot = bridge.snapshot();
      if (!snapshot)
        return;
      const text = JSON.stringify(snapshot);
      if (force || text !== lastState) {
        lastState = text;
        send("state", { scene: snapshot, running: bridge.running, canPlay: bridge.canPlay, edited: changed });
      } else
        send("playback", { running: bridge.running, canPlay: bridge.canPlay });
    }
    async function capture(requestId) {
      if (!ready || closed)
        return;
      if (capturing) {
        if (requestId)
          pendingCapture = requestId;
        return;
      }
      capturing = true;
      try {
        const area = rootEl.querySelector(".sim-visuals") || rootEl, viewport = document.getElementById("simViewport"), width = Math.max(640, Math.min(1200, area.clientWidth)), height = Math.max(400, Math.min(1500, area.scrollHeight + 28));
        let src;
        try {
          src = await Promise.race([toPng(area, { backgroundColor: "#ffffff", pixelRatio: 1, width: area.clientWidth, height: Math.min(1500, area.scrollHeight), cacheBust: false, skipFonts: true }), new Promise((_, reject) => setTimeout(() => reject(Error("Snapshot timeout")), 1200))]);
        } catch {
          const c = document.createElement("canvas");
          c.width = width;
          c.height = height;
          const g = c.getContext("2d");
          g.fillStyle = "#fff";
          g.fillRect(0, 0, width, height);
          g.fillStyle = "#173f42";
          g.font = "bold 22px sans-serif";
          g.fillText(item.title, 20, 30);
          const box = area.getBoundingClientRect(), k = width / box.width;
          for (const cv of area.querySelectorAll("canvas")) {
            const r = cv.getBoundingClientRect();
            try {
              g.drawImage(cv, (r.left - box.left) * k, (r.top - box.top) * k + 45, r.width * k, r.height * k);
            } catch {
            }
          }
          src = c.toDataURL("image/png");
        }
        if (src.length < 15e6)
          send("preview", { src, scene: bridge.snapshot(), requestId });
      } catch (e) {
        send("capture-error", { requestId, message: "Kh\xF4ng ch\u1EE5p \u0111\u01B0\u1EE3c h\xECnh m\xF4 ph\u1ECFng." });
      } finally {
        capturing = false;
        if (pendingCapture) {
          const next = pendingCapture;
          pendingCapture = void 0;
          capture(next);
        }
      }
    }
    function onChange() {
      if (ready) {
        const value = bridge.running + ":" + bridge.canPlay;
        if (value !== lastPlayback) {
          lastPlayback = value;
          send("playback", { running: bridge.running, canPlay: bridge.canPlay });
        }
      }
      changed = true;
      clearTimeout(timer);
      timer = setTimeout(() => {
        announce(true);
        capture();
      }, 450);
    }
    function initialize(value) {
      const safe = import_physics_catalog2.default.clean(value);
      if (!safe || safe.id !== id || !modules[id]) {
        send("error", { message: "D\u1EEF li\u1EC7u m\xF4 ph\u1ECFng kh\xF4ng h\u1EE3p l\u1EC7." });
        return;
      }
      scene2 = safe;
      ready = false;
      root.render(null);
      bridge.start(scene2, onChange);
      const Sim = modules[id], props = { title: item.title, description: item.description, onComplete: () => {
      }, interactiveQuestions: [] };
      root.render(/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Boundary, { children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Sim, { ...props }) }, Date.now()));
      clearTimeout(restoreTimer);
      restoreTimer = setTimeout(() => {
        bridge.restoreRefs();
        fitVisuals();
        ready = true;
        changed = false;
        status.textContent = item.illustrative ? "M\xF4 h\xECnh minh h\u1ECDa \xB7 \u0111\u1EA1i l\u01B0\u1EE3ng theo thang \u0111o c\u1EE7a m\xF4 h\xECnh" : "Thay \u0111\u1ED5i tham s\u1ED1 ho\u1EB7c k\xE9o tr\u1EF1c ti\u1EBFp trong m\xF4 ph\u1ECFng";
        announce(true);
        capture();
        send("mounted", { illustrative: item.illustrative });
      }, 350);
    }
    addEventListener("message", (e) => {
      if (e.source !== parent || !e.data || e.data.nbPhysics !== 1 || e.data.token !== token)
        return;
      if (e.data.type === "init")
        initialize(e.data.scene);
      else if (e.data.type === "pause") {
        bridge.pause();
        setTimeout(() => announce(true), 0);
      } else if (e.data.type === "play") {
        const native = rootEl.querySelector("button[data-nb-play]");
        if (native && !bridge.running)
          native.click();
        else
          bridge.play();
      } else if (e.data.type === "help") {
        const help = rootEl.querySelector(".sim-help");
        if (help) {
          help.open = !help.open;
          help.scrollIntoView({ block: "nearest", behavior: "smooth" });
        }
      } else if (e.data.type === "capture") {
        announce(true);
        capture(e.data.requestId);
      } else if (e.data.type === "reset")
        initialize({ ...scene2, states: {}, refs: {} });
      else if (e.data.type === "dispose") {
        closed = true;
        clearTimeout(timer);
        clearTimeout(restoreTimer);
        cancelAnimationFrame(fitRaf);
        resize.disconnect();
        mutations.disconnect();
        root.unmount();
      }
    });
    addEventListener("keydown", (e) => {
      if (e.key === "Escape") {
        e.preventDefault();
        send("close");
      } else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "s") {
        e.preventDefault();
        send("save");
      } else if ((e.ctrlKey || e.metaKey) && ["z", "y"].includes(e.key.toLowerCase()) && !/INPUT|TEXTAREA|SELECT/.test(e.target?.tagName)) {
        e.preventDefault();
        send("history", { redo: e.key.toLowerCase() === "y" || e.shiftKey });
      }
    });
    document.addEventListener("visibilitychange", () => {
      if (document.hidden) {
        bridge.pause();
        announce(true);
      }
    });
    const clock = setInterval(() => announce(), 900);
    addEventListener("pagehide", () => {
      clearInterval(clock);
      clearTimeout(timer);
      clearTimeout(restoreTimer);
      root.unmount();
    });
    if (!item || !modules[id]) {
      status.textContent = "Kh\xF4ng t\xECm th\u1EA5y m\xF4 ph\u1ECFng.";
      send("error", { message: status.textContent });
      return;
    }
    document.title = item.title + " \xB7 NoteBoard Ink";
    send("ready");
    window.NBPhysicsPlayer = { get scene() {
      return bridge.snapshot();
    }, get state() {
      return bridge.state;
    }, get ready() {
      return ready;
    }, pause: () => bridge.pause() };
  }

  // physics/components/ForceVectorSimulation.tsx
  var import_react3 = __toESM(require_react());

  // physics/vendor/simulations/canvasUtil.ts
  function prepCanvas(canvas, w, h) {
    if (!canvas)
      return null;
    const ctx = canvas.getContext("2d");
    if (!ctx)
      return null;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const cw = canvas.clientWidth || w;
    const ch = canvas.clientHeight || h;
    canvas.dataset.logicalWidth = String(w);
    canvas.dataset.logicalHeight = String(h);
    if (!canvas.style.width)
      canvas.style.width = "100%";
    if (!canvas.style.height) {
      canvas.style.height = "auto";
      canvas.style.aspectRatio = w + "/" + h;
    }
    const bw = Math.max(1, Math.round(cw * dpr));
    const bh = Math.max(1, Math.round(ch * dpr));
    if (canvas.width !== bw || canvas.height !== bh) {
      canvas.width = bw;
      canvas.height = bh;
    }
    ctx.setTransform(bw / w, 0, 0, bh / h, 0, 0);
    ctx.clearRect(0, 0, w, h);
    return ctx;
  }

  // physics/components/ForceVectorSimulation.tsx
  var import_jsx_runtime2 = __toESM(require_jsx_runtime());
  var W = 680;
  var H = 440;
  var S = 9;
  var O = { x: W / 2, y: H / 2 };
  var MAX = 10;
  var mag = (v) => Math.hypot(v.x, v.y);
  var angle = (v) => Math.atan2(v.y, v.x) * 180 / Math.PI;
  var polar = (r, deg) => ({ x: r * Math.cos(deg * Math.PI / 180), y: r * Math.sin(deg * Math.PI / 180) });
  var sum = (a, b) => ({ x: a.x + b.x, y: a.y + b.y });
  function ForceVectorSimulation({ title = "T\u1ED5ng h\u1EE3p & ph\xE2n t\xEDch l\u1EF1c", description = "K\xE9o c\xE1c n\xFAt tr\xF2n \u1EDF \u0111\u1EA7u l\u1EF1c ho\u1EB7c d\xF9ng thanh tr\u01B0\u1EE3t \u0111\u1EC3 \u0111\u1ED5i \u0111\u1ED9 l\u1EDBn v\xE0 h\u01B0\u1EDBng." }) {
    const cv = useSimRef("ForceVectorSimulation.cv", null), dragRef = useSimRef("ForceVectorSimulation.dragRef", null);
    const [mode, setMode] = useSimState("ForceVectorSimulation.mode", "compose"), [f1, setF1] = useSimState("ForceVectorSimulation.f1", { x: 6, y: 0 }), [f2, setF2] = useSimState("ForceVectorSimulation.f2", { x: 0, y: 5 }), [fA, setFA] = useSimState("ForceVectorSimulation.fA", { x: 7, y: 4 }), [axisDeg, setAxisDeg] = useSimState("ForceVectorSimulation.axisDeg", 0);
    const F = mode === "compose" ? sum(f1, f2) : fA, r = axisDeg * Math.PI / 180, u = { x: Math.cos(r), y: Math.sin(r) }, v = { x: -Math.sin(r), y: Math.cos(r) }, fx = F.x * u.x + F.y * u.y, fy = F.x * v.x + F.y * v.y;
    const draw = (0, import_react3.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W, H);
      if (!ctx)
        return;
      ctx.fillStyle = "#f8fafc";
      ctx.fillRect(0, 0, W, H);
      ctx.strokeStyle = "#e6edf2";
      ctx.lineWidth = 1;
      for (let x = O.x % 36; x < W; x += 36) {
        ctx.beginPath();
        ctx.moveTo(x, 0);
        ctx.lineTo(x, H);
        ctx.stroke();
      }
      for (let y = O.y % 36; y < H; y += 36) {
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(W, y);
        ctx.stroke();
      }
      const pt = (a) => ({ x: O.x + a.x * S, y: O.y - a.y * S });
      const line = (a, b, color = "#8ea4b5", dash = true) => {
        const p = pt(a), q = pt(b);
        ctx.strokeStyle = color;
        ctx.lineWidth = 1.5;
        ctx.setLineDash(dash ? [6, 5] : []);
        ctx.beginPath();
        ctx.moveTo(p.x, p.y);
        ctx.lineTo(q.x, q.y);
        ctx.stroke();
        ctx.setLineDash([]);
      };
      const arrow = (a, color, label, handle = false) => {
        const p = pt(a), m = mag(a), dx = m ? a.x / m : 1, dy = m ? -a.y / m : 0;
        ctx.strokeStyle = color;
        ctx.fillStyle = color;
        ctx.lineWidth = handle ? 3 : 4.5;
        ctx.beginPath();
        ctx.moveTo(O.x, O.y);
        ctx.lineTo(p.x, p.y);
        ctx.stroke();
        if (m > 0.02) {
          ctx.beginPath();
          ctx.moveTo(p.x, p.y);
          ctx.lineTo(p.x - 12 * dx + 6 * dy, p.y - 12 * dy - 6 * dx);
          ctx.lineTo(p.x - 12 * dx - 6 * dy, p.y - 12 * dy + 6 * dx);
          ctx.closePath();
          ctx.fill();
        }
        if (handle) {
          const radius = Math.min(18, Math.max(8, 9 * W / (cv.current?.clientWidth || W)));
          ctx.beginPath();
          ctx.arc(p.x, p.y, radius, 0, 2 * Math.PI);
          ctx.fillStyle = "#fff";
          ctx.fill();
          ctx.strokeStyle = color;
          ctx.lineWidth = 3;
          ctx.stroke();
        }
        let base = "F", sub = "";
        if (label === "F\u2081" || label === "F1")
          sub = "1";
        else if (label === "F\u2082" || label === "F2")
          sub = "2";
        else if (label === "F\u2093" || label === "Fx")
          sub = "x";
        else if (label === "F\u1D67" || label === "Fy")
          sub = "y";
        else if (label.startsWith("F"))
          sub = label.slice(1);
        else
          base = label;
        const lx = Math.max(10, Math.min(W - 55, p.x + (dx < -0.3 ? -32 : 13)));
        const ly = Math.max(22, Math.min(H - 12, p.y + (dy > 0.4 ? 20 : -12)));
        ctx.save();
        ctx.fillStyle = color;
        ctx.strokeStyle = color;
        ctx.font = "bold italic 18px system-ui, -apple-system, sans-serif";
        ctx.fillText(base, lx, ly);
        const wB = ctx.measureText(base).width;
        if (sub) {
          ctx.font = "bold italic 12px system-ui, -apple-system, sans-serif";
          ctx.fillText(sub, lx + wB + 1, ly + 3);
        }
        const ovy = ly - 16;
        ctx.lineWidth = 1.6;
        ctx.lineCap = "round";
        ctx.beginPath();
        ctx.moveTo(lx - 1, ovy);
        ctx.lineTo(lx + wB + 1.5, ovy);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(lx + wB + 3, ovy);
        ctx.lineTo(lx + wB - 2.5, ovy - 3.2);
        ctx.lineTo(lx + wB - 2.5, ovy + 3.2);
        ctx.closePath();
        ctx.fill();
        ctx.restore();
      };
      line({ x: -35, y: 0 }, { x: 35, y: 0 }, "#c4d3dc", false);
      line({ x: 0, y: -23 }, { x: 0, y: 23 }, "#c4d3dc", false);
      if (mode === "compose") {
        const total = sum(f1, f2);
        line(f1, total);
        line(f2, total);
        arrow(total, "#7c3aed", "F");
        arrow(f1, "#2563eb", "F\u2081", true);
        arrow(f2, "#0891b2", "F\u2082", true);
      } else {
        const forceU = fA.x * u.x + fA.y * u.y, forceV = fA.x * v.x + fA.y * v.y, fu = { x: u.x * forceU, y: u.y * forceU }, fv = { x: v.x * forceV, y: v.y * forceV };
        line({ x: -22 * u.x, y: -22 * u.y }, { x: 22 * u.x, y: 22 * u.y }, "#e8a220");
        line({ x: -22 * v.x, y: -22 * v.y }, { x: 22 * v.x, y: 22 * v.y }, "#e8a220");
        line(fu, fA);
        line(fv, fA);
        arrow(fu, "#dc2626", "F\u2093");
        arrow(fv, "#0891b2", "F\u1D67");
        arrow(fA, "#7c3aed", "F", true);
      }
      ctx.fillStyle = "#334155";
      ctx.beginPath();
      ctx.arc(O.x, O.y, 4, 0, 2 * Math.PI);
      ctx.fill();
      ctx.font = "12px system-ui";
      ctx.fillText("O", O.x - 17, O.y + 18);
      ctx.fillStyle = "#64748b";
      ctx.fillText("K\xE9o n\xFAt tr\xF2n \u1EDF \u0111\u1EA7u vect\u01A1", 16, H - 16);
    }, [mode, f1, f2, fA, axisDeg]);
    (0, import_react3.useEffect)(() => {
      draw();
      const ro = new ResizeObserver(() => draw());
      if (cv.current)
        ro.observe(cv.current);
      return () => ro.disconnect();
    }, [draw]);
    function position(e) {
      const b = cv.current.getBoundingClientRect();
      return { x: ((e.clientX - b.left) / b.width * W - O.x) / S, y: (O.y - (e.clientY - b.top) / b.height * H) / S };
    }
    function down(e) {
      const p = position(e), b = cv.current.getBoundingClientRect(), list = mode === "compose" ? [["f1", f1], ["f2", f2]] : [["fA", fA]];
      let best = Infinity, id = null;
      for (const [key, a] of list) {
        const d = Math.hypot((a.x - p.x) * S * b.width / W, (a.y - p.y) * S * b.height / H);
        if (d < Math.max(24, e.pointerType === "touch" ? 34 : 24) && d < best) {
          best = d;
          id = key;
        }
      }
      dragRef.current = id;
      if (id) {
        e.preventDefault();
        e.currentTarget.setPointerCapture(e.pointerId);
      }
    }
    function move(e) {
      if (!dragRef.current)
        return;
      let p = position(e), m = mag(p);
      if (m > MAX)
        p = { x: p.x / m * MAX, y: p.y / m * MAX };
      (dragRef.current === "f1" ? setF1 : dragRef.current === "f2" ? setF2 : setFA)(p);
    }
    const VecSym = ({ name }) => {
      const sub = name === "F\u2081" || name === "F1" ? "1" : name === "F\u2082" || name === "F2" ? "2" : name === "F\u2093" || name === "Fx" ? "x" : name === "F\u1D67" || name === "Fy" ? "y" : name.slice(1);
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("span", { style: { display: "inline-flex", alignItems: "baseline", fontWeight: "bold" }, children: [
        /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("span", { style: { position: "relative", display: "inline-block", fontStyle: "italic", paddingTop: "4px", paddingRight: "1px" }, children: [
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("span", { style: { position: "absolute", top: "-3px", left: 0, right: 0, fontSize: "11px", fontStyle: "normal", lineHeight: 1, textAlign: "center" }, children: "\u2192" }),
          "F"
        ] }),
        sub && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("sub", { style: { fontStyle: "italic", fontSize: "0.75em", marginLeft: "1px" }, children: sub })
      ] });
    };
    const slider = (name, value, min, max, step, set, unit) => /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("label", { children: [
      /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("span", { children: name }),
      /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("output", { children: [
        value.toFixed(step < 1 ? 1 : 0),
        unit
      ] }),
      /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("input", { "aria-label": name, type: "range", min, max, step, value, onChange: (e) => set(+e.target.value) })
    ] });
    const forceControls = (name, a, set) => /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("fieldset", { children: [
      /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("legend", { children: /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(VecSym, { name }) }),
      slider("\u0110\u1ED9 l\u1EDBn " + name, mag(a), 0, 10, 0.1, (n) => set(polar(n, angle(a))), " N"),
      slider("H\u01B0\u1EDBng " + name, angle(a), -180, 180, 1, (n) => set(polar(mag(a), n)), "\xB0")
    ] });
    return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("canvas", { ref: cv, width: W, height: H, onPointerDown: down, onPointerMove: move, onPointerUp: (e) => {
        move(e);
        dragRef.current = null;
      }, onPointerCancel: () => dragRef.current = null, "aria-label": "K\xE9o \u0111\u1EA7u c\xE1c vect\u01A1 l\u1EF1c" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { className: "force-modes", children: [
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("button", { "aria-pressed": mode === "compose", onClick: () => setMode("compose"), children: "T\u1ED5ng h\u1EE3p l\u1EF1c" }),
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("button", { "aria-pressed": mode === "decompose", onClick: () => setMode("decompose"), children: "Ph\xE2n t\xEDch l\u1EF1c" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { className: "force-result", role: "status", children: [
          /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("strong", { children: [
            "F = ",
            mag(F).toFixed(2),
            " N"
          ] }),
          mode === "compose" ? /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)(import_jsx_runtime2.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("span", { style: { display: "inline-block", margin: "0 4px", color: "#7c3aed" }, children: [
              "(",
              /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(VecSym, { name: "F" }),
              " = ",
              /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(VecSym, { name: "F\u2081" }),
              " + ",
              /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(VecSym, { name: "F\u2082" }),
              ")"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("span", { children: [
              "F\u2093 = ",
              F.x.toFixed(2),
              " N \xB7 F\u1D67 = ",
              F.y.toFixed(2),
              " N"
            ] })
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)(import_jsx_runtime2.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("span", { style: { display: "inline-block", margin: "0 4px", color: "#7c3aed" }, children: [
              "(",
              /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(VecSym, { name: "F" }),
              " = ",
              /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(VecSym, { name: "F\u2093" }),
              " + ",
              /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(VecSym, { name: "F\u1D67" }),
              ")"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("span", { children: [
              "F\u2093\u2032 = ",
              fx.toFixed(2),
              " N \xB7 F\u1D67\u2032 = ",
              fy.toFixed(2),
              " N"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { className: "force-parameters", children: mode === "compose" ? /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)(import_jsx_runtime2.Fragment, { children: [
          forceControls("F\u2081", f1, setF1),
          forceControls("F\u2082", f2, setF2)
        ] }) : /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)(import_jsx_runtime2.Fragment, { children: [
          forceControls("F", fA, setFA),
          slider("G\xF3c tr\u1EE5c Ox\u2032", axisDeg, 0, 180, 1, setAxisDeg, "\xB0")
        ] }) }),
        mode === "compose" && /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { className: "force-presets", children: [
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("button", { onClick: () => setF2(polar(mag(f2), angle(f1))), children: "C\xF9ng chi\u1EC1u" }),
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("button", { onClick: () => setF2(polar(mag(f2), angle(f1) + 90)), children: "Vu\xF4ng g\xF3c" }),
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("button", { onClick: () => setF2(polar(mag(f2), angle(f1) + 180)), children: "Ng\u01B0\u1EE3c chi\u1EC1u" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("h4", { children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("p", { children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("p", { children: "\u0110\xE2y l\xE0 m\xF4 h\xECnh l\u1EF1c t\u0129nh: \u0111\u1ED5i l\u1EF1c b\u1EB1ng c\xE1ch k\xE9o \u0111\u1EA7u vect\u01A1 ho\u1EB7c c\xE1c thanh tr\u01B0\u1EE3t. Kh\xF4ng c\u1EA7n b\u1EA5m Ch\u1EA1y. H\u1EE3p l\u1EF1c m\xE0u t\xEDm \u0111\u01B0\u1EE3c t\xEDnh ngay t\u1EEB t\u1ED5ng hai vect\u01A1." })
        ] })
      ] })
    ] });
  }

  // physics/components/InclinedPlaneSimulation.tsx
  var import_react4 = __toESM(require_react());
  var import_jsx_runtime3 = __toESM(require_jsx_runtime());
  var G = 9.8;
  var W2 = 680;
  var H2 = 400;
  var LPIX = 300;
  var SLOPE_M = 3.75;
  var SCALE = LPIX / SLOPE_M;
  var FOOT_X = 360;
  var GROUND_Y = H2 - 56;
  var HALF = 22;
  var GX_MAX = (W2 - 28 - (FOOT_X + HALF)) / SCALE;
  var GX_OFF = (W2 + HALF * 2 - FOOT_X) / SCALE;
  var InclinedPlaneSimulation = ({ title = "M\u1EB7t ph\u1EB3ng nghi\xEAng \u2014 ph\xE2n t\xEDch l\u1EF1c & ma s\xE1t", description = "D\u1ED1c nghi\xEAng b\xEAn tr\xE1i: v\u1EADt tr\u01B0\u1EE3t xu\u1ED1ng SANG PH\u1EA2I r\u1ED3i ti\u1EBFp t\u1EE5c tr\u01B0\u1EE3t tr\xEAn m\u1EB7t ph\u1EB3ng ngang (c\xF9ng \u03BC) \u0111\u1EBFn khi d\u1EEBng. Tr\xEAn d\u1ED1c hi\u1EC7n P\u2192P\u209C,P\u2099 (h\xECnh b\xECnh h\xE0nh) + F\u2098\u209B; tr\xEAn m\u1EB7t ngang hi\u1EC7n P, N + F\u2098\u209B. K\xE9o \u0111\u1EB7t v\u1EADt r\u1ED3i th\u1EA3 tay.", onComplete }) => {
    const cv = useSimRef("InclinedPlaneSimulation.cv", null);
    const infoRef = useSimRef("InclinedPlaneSimulation.infoRef", null);
    const [angle2, setAngle] = useSimState("InclinedPlaneSimulation.angle", 30);
    const [m, setM] = useSimState("InclinedPlaneSimulation.m", 2);
    const [mu, setMu] = useSimState("InclinedPlaneSimulation.mu", 0.3);
    const [v0, setV0] = useSimState("InclinedPlaneSimulation.v0", 0);
    const [playing, setPlaying] = useSimState("InclinedPlaneSimulation.playing", false);
    const phaseRef = useSimRef("InclinedPlaneSimulation.phaseRef", "incline");
    const sRef = useSimRef("InclinedPlaneSimulation.sRef", 0.7);
    const gxRef = useSimRef("InclinedPlaneSimulation.gxRef", 0);
    const vRef = useSimRef("InclinedPlaneSimulation.vRef", 0);
    const aRef = useSimRef("InclinedPlaneSimulation.aRef", 0);
    const param = useSimRef("InclinedPlaneSimulation.param", { angle: angle2, m, mu });
    const raf = useSimRef("InclinedPlaneSimulation.raf", null);
    const lastTs = useSimRef("InclinedPlaneSimulation.lastTs", null);
    const playRef = useSimRef("InclinedPlaneSimulation.playRef", false);
    const dragRef = useSimRef("InclinedPlaneSimulation.dragRef", false);
    const firstRef = useSimRef("InclinedPlaneSimulation.firstRef", false);
    (0, import_react4.useEffect)(() => {
      param.current = { angle: angle2, m, mu };
    }, [angle2, m, mu]);
    (0, import_react4.useEffect)(() => {
      playRef.current = playing;
    }, [playing]);
    const fire = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const geom = (deg) => {
      const r = deg * Math.PI / 180;
      const A = { x: FOOT_X, y: GROUND_Y };
      const C3 = { x: FOOT_X - LPIX * Math.cos(r), y: GROUND_Y - LPIX * Math.sin(r) };
      const down = { x: Math.cos(r), y: Math.sin(r) };
      const nrm = { x: Math.sin(r), y: -Math.cos(r) };
      return { r, A, C: C3, down, nrm };
    };
    const blockCenter = (deg) => {
      const { A, C: C3, down, nrm } = geom(deg);
      if (phaseRef.current === "incline") {
        const sp = Math.min(SLOPE_M, Math.max(0, sRef.current)) * SCALE;
        return { x: C3.x + down.x * sp + nrm.x * HALF, y: C3.y + down.y * sp + nrm.y * HALF };
      }
      return { x: A.x + Math.max(0, gxRef.current) * SCALE, y: GROUND_Y - HALF };
    };
    const arrow = (ctx, x, y, dx, dy, color, label, dash = false) => {
      const len = Math.hypot(dx, dy);
      if (len < 1)
        return;
      const ux = dx / len, uy = dy / len;
      ctx.strokeStyle = color;
      ctx.fillStyle = color;
      ctx.lineWidth = 3;
      ctx.setLineDash(dash ? [5, 4] : []);
      ctx.beginPath();
      ctx.moveTo(x, y);
      ctx.lineTo(x + dx, y + dy);
      ctx.stroke();
      ctx.setLineDash([]);
      const ah = 9;
      ctx.beginPath();
      ctx.moveTo(x + dx, y + dy);
      ctx.lineTo(x + dx - ah * ux - ah * 0.6 * uy, y + dy - ah * uy + ah * 0.6 * ux);
      ctx.lineTo(x + dx - ah * ux + ah * 0.6 * uy, y + dy - ah * uy - ah * 0.6 * ux);
      ctx.closePath();
      ctx.fill();
      const lx = x + dx + ux * 6 - 4, ly = y + dy + uy * 6 + 5;
      ctx.font = "bold 13px sans-serif";
      ctx.fillStyle = color;
      ctx.fillText(label, lx, ly);
      const tw = ctx.measureText(label).width, ovy = ly - 12;
      ctx.strokeStyle = color;
      ctx.lineWidth = 1.3;
      ctx.beginPath();
      ctx.moveTo(lx - 1, ovy);
      ctx.lineTo(lx + tw + 1, ovy);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(lx + tw + 1, ovy);
      ctx.lineTo(lx + tw - 3, ovy - 2.5);
      ctx.lineTo(lx + tw - 3, ovy + 2.5);
      ctx.closePath();
      ctx.fill();
    };
    const draw = (0, import_react4.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W2, H2);
      if (!ctx)
        return;
      const { r, A, C: C3, down, nrm } = geom(angle2);
      ctx.clearRect(0, 0, W2, H2);
      ctx.fillStyle = "#f8fafc";
      ctx.fillRect(0, 0, W2, H2);
      ctx.fillStyle = "#e2e8f0";
      ctx.fillRect(0, GROUND_Y, W2, H2 - GROUND_Y);
      ctx.beginPath();
      ctx.moveTo(C3.x, C3.y);
      ctx.lineTo(C3.x, GROUND_Y);
      ctx.lineTo(A.x, A.y);
      ctx.closePath();
      ctx.fillStyle = "#cbd5e1";
      ctx.fill();
      ctx.strokeStyle = "#94a3b8";
      ctx.lineWidth = 2;
      ctx.stroke();
      ctx.strokeStyle = "#7c3aed";
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(A.x, A.y, 40, Math.PI, Math.PI + r);
      ctx.stroke();
      ctx.fillStyle = "#6d28d9";
      ctx.font = "bold 14px sans-serif";
      ctx.fillText(`\u03B1=${angle2}\xB0`, A.x - 82, A.y - 14);
      const bc = blockCenter(angle2);
      const onInc = phaseRef.current === "incline";
      const v = vRef.current;
      ctx.save();
      ctx.translate(bc.x, bc.y);
      if (onInc)
        ctx.rotate(r);
      const g = ctx.createLinearGradient(0, -HALF, 0, HALF);
      g.addColorStop(0, "#fcd34d");
      g.addColorStop(1, "#d97706");
      ctx.fillStyle = g;
      ctx.fillRect(-HALF, -HALF, HALF * 2, HALF * 2);
      ctx.strokeStyle = "#92400e";
      ctx.lineWidth = 2;
      ctx.strokeRect(-HALF, -HALF, HALF * 2, HALF * 2);
      ctx.fillStyle = "#fff";
      ctx.font = "bold 12px sans-serif";
      ctx.textAlign = "center";
      ctx.fillText(`${m}kg`, 0, 4);
      ctx.textAlign = "left";
      ctx.restore();
      const P = m * G, vlen = Math.min(3, (H2 - 28 - bc.y) / P, (bc.y - 28) / P);
      if (infoRef.current) {
        const N = onInc ? P * Math.cos(r) : P;
        infoRef.current.textContent = `P = ${P.toFixed(1)} N \xB7 N = ${N.toFixed(1)} N
P\u209C = ${(onInc ? P * Math.sin(r) : 0).toFixed(1)} N \xB7 \u03BCN = ${(mu * N).toFixed(1)} N
v = ${v.toFixed(2)} m/s \xB7 a = ${aRef.current.toFixed(2)} m/s\xB2`;
      }
      const tx = 16, ty = 24;
      if (onInc) {
        const N = P * Math.cos(r), Pt = P * Math.sin(r), Fk = mu * N;
        const tPt = { x: bc.x + down.x * Pt * vlen, y: bc.y + down.y * Pt * vlen };
        const tPn = { x: bc.x - nrm.x * N * vlen, y: bc.y - nrm.y * N * vlen };
        const tP = { x: bc.x, y: bc.y + P * vlen };
        ctx.strokeStyle = "#a78bfa";
        ctx.lineWidth = 1.5;
        ctx.setLineDash([5, 4]);
        ctx.beginPath();
        ctx.moveTo(tPt.x, tPt.y);
        ctx.lineTo(tP.x, tP.y);
        ctx.lineTo(tPn.x, tPn.y);
        ctx.stroke();
        ctx.setLineDash([]);
        arrow(ctx, bc.x, bc.y, down.x * Pt * vlen, down.y * Pt * vlen, "#dc2626", "P\u209C");
        arrow(ctx, bc.x, bc.y, -nrm.x * N * vlen, -nrm.y * N * vlen, "#0891b2", "P\u2099");
        arrow(ctx, bc.x, bc.y, nrm.x * N * vlen, nrm.y * N * vlen, "#0d9488", "N");
        arrow(ctx, bc.x, bc.y, 0, P * vlen, "#b45309", "P", true);
        let dir = 0, fm = 0;
        if (Math.abs(v) > 0.02) {
          dir = -Math.sign(v);
          fm = Fk;
        } else if (Pt > 0.01) {
          dir = -1;
          fm = Math.min(Fk, Pt);
        }
        if (dir !== 0)
          arrow(ctx, bc.x, bc.y, down.x * fm * vlen * dir, down.y * fm * vlen * dir, "#059669", "F\u2098\u209B");
        ctx.font = "bold 13px sans-serif";
        ctx.fillStyle = "#0f172a";
        ctx.fillText(`P = m\xB7g = ${P.toFixed(1)} N`, tx, ty);
        ctx.fillStyle = "#dc2626";
        ctx.fillText(`P\u209C = P\xB7sin\u03B1 = ${Pt.toFixed(1)} N`, tx, ty + 20);
        ctx.fillStyle = "#0891b2";
        ctx.fillText(`P\u2099 = N = P\xB7cos\u03B1 = ${N.toFixed(1)} N`, tx, ty + 40);
        ctx.fillStyle = "#059669";
        ctx.fillText(`F\u2098\u209B(max) = \u03BC\xB7N = ${Fk.toFixed(1)} N`, tx, ty + 60);
        ctx.font = "bold 14px sans-serif";
        if (Math.abs(v) > 0.02) {
          ctx.fillStyle = "#7c3aed";
          ctx.fillText(`v = ${v.toFixed(2)} m/s \xB7 a = ${aRef.current.toFixed(2)} m/s\xB2`, tx, ty + 86);
        } else if (Pt > Fk) {
          ctx.fillStyle = "#dc2626";
          ctx.fillText("P\u209C > F\u2098\u209B \u2192 TH\u1EA2 TAY V\u1EACT TR\u01AF\u1EE2T XU\u1ED0NG (sang ph\u1EA3i)", tx, ty + 86);
        } else {
          ctx.fillStyle = "#16a34a";
          ctx.fillText("P\u209C \u2264 F\u2098\u209B \u2192 V\u1EACT \u0110\u1EE8NG Y\xCAN", tx, ty + 86);
        }
      } else {
        const N = P, Fk = mu * P;
        arrow(ctx, bc.x, bc.y, 0, P * vlen, "#b45309", "P");
        arrow(ctx, bc.x, bc.y, 0, -N * vlen, "#0891b2", "N");
        if (Math.abs(v) > 0.02 && Fk > 0.01)
          arrow(ctx, bc.x, bc.y, -Math.sign(v) * Fk * vlen, 0, "#059669", "F\u2098\u209B");
        ctx.font = "bold 13px sans-serif";
        ctx.fillStyle = "#0f172a";
        ctx.fillText(`M\u1EB7t ph\u1EB3ng ngang: N = P = ${P.toFixed(1)} N`, tx, ty);
        ctx.fillStyle = "#059669";
        ctx.fillText(`F\u2098\u209B = \u03BC\xB7N = ${Fk.toFixed(1)} N (c\u1EA3n chuy\u1EC3n \u0111\u1ED9ng)`, tx, ty + 20);
        ctx.font = "bold 14px sans-serif";
        if (Math.abs(v) > 0.02) {
          ctx.fillStyle = "#7c3aed";
          ctx.fillText(`v = ${v.toFixed(2)} m/s \xB7 a = ${aRef.current.toFixed(2)} m/s\xB2 (${Fk > 0.01 ? "ch\u1EADm d\u1EA7n" : "\u0111\u1EC1u \u2014 kh\xF4ng ma s\xE1t"})`, tx, ty + 46);
        } else {
          ctx.fillStyle = "#16a34a";
          ctx.fillText("V\u1EADt \u0111\xE3 D\u1EEANG (ma s\xE1t tri\u1EC7t ti\xEAu v\u1EADn t\u1ED1c)", tx, ty + 46);
        }
      }
    }, [angle2, m, mu]);
    const loop = (0, import_react4.useCallback)((ts) => {
      if (!playRef.current)
        return;
      if (lastTs.current == null)
        lastTs.current = ts;
      let dt = (ts - lastTs.current) / 1e3;
      lastTs.current = ts;
      dt = Math.min(dt, 0.03);
      const { angle: aDeg, m: mm, mu: mmu } = param.current;
      const r = aDeg * Math.PI / 180;
      let v = vRef.current, a = 0, stop = false;
      if (phaseRef.current === "incline") {
        const Pt = mm * G * Math.sin(r), Fk = mmu * mm * G * Math.cos(r);
        if (Math.abs(v) < 0.02) {
          if (Pt > Fk) {
            a = (Pt - Fk) / mm;
            v = 0;
          } else {
            a = 0;
            v = 0;
            stop = true;
          }
        } else
          a = (Pt - Fk * Math.sign(v)) / mm;
        v += a * dt;
        let s = sRef.current + v * dt;
        if (s <= 0) {
          s = 0;
          v = 0;
          stop = true;
        }
        if (s >= SLOPE_M) {
          sRef.current = SLOPE_M;
          phaseRef.current = "ground";
          gxRef.current = 0;
          vRef.current = Math.max(0, v);
          aRef.current = a;
          draw();
          if (playRef.current)
            raf.current = requestAnimationFrame(loop);
          return;
        }
        sRef.current = s;
        vRef.current = v;
        aRef.current = a;
      } else {
        const Fk = mmu * mm * G;
        if (Math.abs(v) < 0.02) {
          a = 0;
          v = 0;
          stop = true;
        } else {
          a = -(Fk / mm) * Math.sign(v);
          const nv = v + a * dt;
          if (Math.sign(nv) !== Math.sign(v)) {
            v = 0;
            stop = true;
          } else
            v = nv;
        }
        const gx = gxRef.current + v * dt;
        gxRef.current = gx;
        vRef.current = v;
        aRef.current = a;
        if (gx >= GX_OFF)
          stop = true;
      }
      draw();
      if (stop) {
        setPlaying(false);
        return;
      }
      if (playRef.current)
        raf.current = requestAnimationFrame(loop);
    }, [draw]);
    (0, import_react4.useEffect)(() => {
      if (playing) {
        lastTs.current = null;
        raf.current = requestAnimationFrame(loop);
      }
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [playing, loop]);
    (0, import_react4.useEffect)(() => {
      if (!playing)
        draw();
    }, [draw, playing]);
    const toLogic = (e) => {
      const rc = cv.current.getBoundingClientRect();
      return { x: (e.clientX - rc.left) / rc.width * W2, y: (e.clientY - rc.top) / rc.height * H2 };
    };
    const onDown = (e) => {
      const p = toLogic(e);
      const bc = blockCenter(angle2);
      if (Math.hypot(p.x - bc.x, p.y - bc.y) < 42) {
        dragRef.current = true;
        setPlaying(false);
        fire();
        e.target.setPointerCapture?.(e.pointerId);
      }
    };
    const onMove = (e) => {
      if (!dragRef.current)
        return;
      const p = toLogic(e);
      const { A, C: C3, down } = geom(angle2);
      if (p.x <= A.x + 2) {
        phaseRef.current = "incline";
        const sp = (p.x - C3.x) * down.x + (p.y - C3.y) * down.y;
        sRef.current = Math.min(SLOPE_M, Math.max(0, sp / SCALE));
      } else {
        phaseRef.current = "ground";
        gxRef.current = Math.min(GX_MAX, Math.max(0, (p.x - A.x) / SCALE));
      }
      vRef.current = 0;
      draw();
    };
    const onUp = () => {
      if (dragRef.current) {
        dragRef.current = false;
        vRef.current = v0;
        setPlaying(true);
      }
    };
    const reset = () => {
      setPlaying(false);
      phaseRef.current = "incline";
      sRef.current = 0.7;
      gxRef.current = 0;
      vRef.current = 0;
      aRef.current = 0;
      draw();
    };
    const run = () => {
      fire();
      vRef.current = v0;
      setPlaying(true);
    };
    const sf = (s) => (v) => {
      fire();
      s(v);
    };
    return /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("canvas", { ref: cv, width: W2, height: H2, onPointerDown: onDown, onPointerMove: onMove, onPointerUp: onUp, onPointerCancel: onUp, className: "w-full rounded-lg border border-gray-100 bg-slate-50 touch-none cursor-grab active:cursor-grabbing" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("div", { className: "flex items-center justify-center gap-2 my-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("button", { onClick: run, className: "px-4 py-2 rounded-xl bg-violet-600 text-white text-sm font-bold hover:bg-violet-700", "data-nb-play": "", children: "\u25B6 Th\u1EA3 / Ch\u1EA1y" }),
          /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("button", { onClick: reset, className: "px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-sm font-bold hover:bg-slate-200", children: "\u21BA \u0110\u1EB7t l\u1EA1i l\xEAn d\u1ED1c" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("output", { ref: infoRef, className: "incline-readout", "aria-label": "S\u1ED1 li\u1EC7u l\u1EF1c v\xE0 chuy\u1EC3n \u0111\u1ED9ng" }),
        /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("div", { className: "grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-2 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("span", { className: "w-28 text-gray-600", children: "G\xF3c \u03B1" }),
            /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("input", { type: "range", min: 5, max: 55, step: 1, value: angle2, onChange: (e) => sf(setAngle)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("span", { className: "w-16 text-right font-semibold", children: [
              angle2,
              "\xB0"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("span", { className: "w-28 text-gray-600", children: "Kh\u1ED1i l\u01B0\u1EE3ng m" }),
            /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("input", { type: "range", min: 0.5, max: 5, step: 0.5, value: m, onChange: (e) => sf(setM)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("span", { className: "w-16 text-right font-semibold", children: [
              m,
              " kg"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("span", { className: "w-28 text-gray-600", children: "H\u1EC7 s\u1ED1 ma s\xE1t \u03BC" }),
            /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("input", { type: "range", min: 0, max: 1, step: 0.05, value: mu, onChange: (e) => sf(setMu)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("span", { className: "w-16 text-right font-semibold", children: mu.toFixed(2) })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("span", { className: "w-28 text-gray-600", children: "V\u1EADn t\u1ED1c \u0111\u1EA7u v\u2080" }),
            /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("input", { type: "range", min: 0, max: 5, step: 0.5, value: v0, onChange: (e) => sf(setV0)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("span", { className: "w-16 text-right font-semibold", children: [
              v0,
              " m/s"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("h4", { className: "font-bold text-violet-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "K\xE9o v\u1EADt \u0111\u1EB7t tr\xEAn d\u1ED1c (ho\u1EB7c tr\xEAn m\u1EB7t ngang b\xEAn ph\u1EA3i) r\u1ED3i th\u1EA3 tay. V\u1EADt tr\u01B0\u1EE3t xu\u1ED1ng d\u1ED1c \u2192 t\u1EDBi ch\xE2n d\u1ED1c ti\u1EBFp t\u1EE5c tr\u01B0\u1EE3t tr\xEAn m\u1EB7t ph\u1EB3ng ngang, ma s\xE1t l\xE0m ch\u1EADm d\u1EA7n r\u1ED3i d\u1EEBng." })
        ] })
      ] })
    ] });
  };
  var InclinedPlaneSimulation_default = InclinedPlaneSimulation;

  // physics/vendor/simulations/CollisionSimulation.tsx
  var import_react6 = __toESM(require_react());

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/lucide-react/dist/esm/createLucideIcon.mjs
  var import_react5 = __toESM(require_react(), 1);

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/lucide-react/dist/esm/defaultAttributes.mjs
  var defaultAttributes = {
    xmlns: "http://www.w3.org/2000/svg",
    width: 24,
    height: 24,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 2,
    strokeLinecap: "round",
    strokeLinejoin: "round"
  };

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/lucide-react/dist/esm/createLucideIcon.mjs
  var toKebabCase = (string) => string.replace(/([a-z0-9])([A-Z])/g, "$1-$2").toLowerCase();
  var createLucideIcon = (iconName, iconNode) => {
    const Component = (0, import_react5.forwardRef)(
      ({ color = "currentColor", size = 24, strokeWidth = 2, absoluteStrokeWidth, children, ...rest }, ref) => (0, import_react5.createElement)(
        "svg",
        {
          ref,
          ...defaultAttributes,
          width: size,
          height: size,
          stroke: color,
          strokeWidth: absoluteStrokeWidth ? Number(strokeWidth) * 24 / Number(size) : strokeWidth,
          className: `lucide lucide-${toKebabCase(iconName)}`,
          ...rest
        },
        [
          ...iconNode.map(([tag, attrs]) => (0, import_react5.createElement)(tag, attrs)),
          ...(Array.isArray(children) ? children : [children]) || []
        ]
      )
    );
    Component.displayName = `${iconName}`;
    return Component;
  };
  var createLucideIcon$1 = createLucideIcon;

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/lucide-react/dist/esm/icons/pause.mjs
  var Pause = createLucideIcon$1("Pause", [
    ["rect", { width: "4", height: "16", x: "6", y: "4", key: "iffhe4" }],
    ["rect", { width: "4", height: "16", x: "14", y: "4", key: "sjin7j" }]
  ]);

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/lucide-react/dist/esm/icons/play.mjs
  var Play = createLucideIcon$1("Play", [
    ["polygon", { points: "5 3 19 12 5 21 5 3", key: "191637" }]
  ]);

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/lucide-react/dist/esm/icons/rotate-ccw.mjs
  var RotateCcw = createLucideIcon$1("RotateCcw", [
    [
      "path",
      { d: "M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8", key: "1357e3" }
    ],
    ["path", { d: "M3 3v5h5", key: "1xhq8a" }]
  ]);

  // ../../../Ebook - Antigravity/ebook.vatli365.vn/node_modules/lucide-react/dist/esm/icons/zap.mjs
  var Zap = createLucideIcon$1("Zap", [
    [
      "polygon",
      { points: "13 2 3 14 12 14 11 22 21 10 12 10 13 2", key: "45s27k" }
    ]
  ]);

  // physics/vendor/simulations/CollisionSimulation.tsx
  var import_jsx_runtime4 = __toESM(require_jsx_runtime());
  var CollisionSimulation = ({ title = "Va ch\u1EA1m & b\u1EA3o to\xE0n \u0111\u1ED9ng l\u01B0\u1EE3ng", description = "Hai xe va ch\u1EA1m tr\xEAn ray. Quan s\xE1t vec t\u01A1 \u0111\u1ED9ng l\u01B0\u1EE3ng \u0111\u1EB7t t\u1EA1i t\xE2m xe v\xE0 \u0111\u1ED3 th\u1ECB v\u1EADn t\u1ED1c theo th\u1EDDi gian (nh\u01B0 c\u1EA3m bi\u1EBFn Pasco).", onComplete }) => {
    const cv = useSimRef("CollisionSimulation.cv", null);
    const gcv = useSimRef("CollisionSimulation.gcv", null);
    const raf = useSimRef("CollisionSimulation.raf", null);
    const lastRef = useSimRef("CollisionSimulation.lastRef", null);
    const b1 = useSimRef("CollisionSimulation.b1", { x: 120, v: 70, m: 3 });
    const b2 = useSimRef("CollisionSimulation.b2", { x: 460, v: 0, m: 3 });
    const x1Init = useSimRef("CollisionSimulation.x1Init", 120), x2Init = useSimRef("CollisionSimulation.x2Init", 460);
    const drag = useSimRef("CollisionSimulation.drag", null);
    const collided = useSimRef("CollisionSimulation.collided", false);
    const inCol = useSimRef("CollisionSimulation.inCol", false), colT = useSimRef("CollisionSimulation.colT", 0);
    const vFrom = useSimRef("CollisionSimulation.vFrom", [0, 0]), vTo = useSimRef("CollisionSimulation.vTo", [0, 0]);
    const tRef = useSimRef("CollisionSimulation.tRef", 0);
    const hist = useSimRef("CollisionSimulation.hist", []);
    const [m1, setM1] = useSimState("CollisionSimulation.m1", 3), [m2, setM2] = useSimState("CollisionSimulation.m2", 3);
    const [v1, setV1] = useSimState("CollisionSimulation.v1", 70), [v2, setV2] = useSimState("CollisionSimulation.v2", 0);
    const [e, setE] = useSimState("CollisionSimulation.e", 1);
    const [running2, setRunning] = useSimState("CollisionSimulation.running", false);
    const [, force] = (0, import_react6.useState)(0);
    const firstRef = useSimRef("CollisionSimulation.firstRef", false);
    const W12 = 680, H12 = 190, GW4 = 680, GH4 = 230, FLOOR = 150;
    const COL_TAU = 0.16;
    const sizeOf = (m) => 26 + m * 8;
    const reset = (0, import_react6.useCallback)(() => {
      setRunning(false);
      collided.current = false;
      inCol.current = false;
      tRef.current = 0;
      b1.current = { x: x1Init.current, v: v1, m: m1 };
      b2.current = { x: x2Init.current, v: v2, m: m2 };
      hist.current = [{ t: 0, v1, v2 }];
      force((n) => n + 1);
    }, [m1, m2, v1, v2]);
    (0, import_react6.useEffect)(() => {
      reset();
    }, [m1, m2, v1, v2]);
    const arrowH = (ctx, x, y, dx, color) => {
      if (Math.abs(dx) < 3)
        return;
      const ex = x + dx, dir = Math.sign(dx);
      ctx.strokeStyle = color;
      ctx.fillStyle = color;
      ctx.lineWidth = 3;
      ctx.lineCap = "round";
      ctx.beginPath();
      ctx.moveTo(x, y);
      ctx.lineTo(ex, y);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(ex, y);
      ctx.lineTo(ex - dir * 9, y - 5);
      ctx.lineTo(ex - dir * 9, y + 5);
      ctx.closePath();
      ctx.fill();
      ctx.lineCap = "butt";
    };
    const draw = (0, import_react6.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.fillStyle = "#e2e8f0";
      ctx.fillRect(0, FLOOR, W12, 6);
      const A = b1.current, B = b2.current;
      const drawBox = (b, color, edge, label, pLabel) => {
        const s = sizeOf(b.m);
        const g = ctx.createLinearGradient(0, FLOOR - s, 0, FLOOR);
        g.addColorStop(0, color);
        g.addColorStop(1, edge);
        ctx.fillStyle = g;
        ctx.fillRect(b.x - s / 2, FLOOR - s, s, s);
        ctx.strokeStyle = edge;
        ctx.lineWidth = 2;
        ctx.strokeRect(b.x - s / 2, FLOOR - s, s, s);
        ctx.fillStyle = "#fff";
        ctx.font = "bold 12px sans-serif";
        ctx.textAlign = "center";
        ctx.fillText(`${b.m}kg`, b.x, FLOOR - 7);
        ctx.textAlign = "left";
        const py = FLOOR - s / 2;
        const plen = Math.sign(b.v) * Math.min(135, Math.abs(b.m * b.v) * 0.24);
        arrowH(ctx, b.x, py, plen, "#7c3aed");
        ctx.fillStyle = "#7c3aed";
        ctx.beginPath();
        ctx.arc(b.x, py, 3.5, 0, Math.PI * 2);
        ctx.fill();
        if (Math.abs(plen) > 3) {
          ctx.font = "bold 12px sans-serif";
          ctx.textAlign = "center";
          ctx.fillText(pLabel, b.x + plen / 2, py - 9);
          ctx.textAlign = "left";
        }
      };
      drawBox(A, "#93c5fd", "#1d4ed8", "", "p\u2081");
      drawBox(B, "#fca5a5", "#b91c1c", "", "p\u2082");
      const p = (A.m * A.v + B.m * B.v).toFixed(0);
      const ke = (0.5 * A.m * A.v * A.v + 0.5 * B.m * B.v * B.v).toFixed(0);
      ctx.fillStyle = "#334155";
      ctx.font = "bold 13px sans-serif";
      ctx.fillText(`\u0110\u1ED9ng l\u01B0\u1EE3ng p = ${p}   |   \u0110\u1ED9ng n\u0103ng W\u0111 = ${ke}`, 16, 22);
    }, []);
    const drawGraph = (0, import_react6.useCallback)(() => {
      const ctx = prepCanvas(gcv.current, GW4, GH4);
      if (!ctx)
        return;
      const x0 = 54, mid = GH4 / 2, plotW = GW4 - x0 - 16, plotH = mid - 18;
      const h = hist.current;
      let mx = 10;
      for (const pt of h)
        mx = Math.max(mx, Math.abs(pt.v1), Math.abs(pt.v2));
      const step = mx <= 100 ? 25 : mx <= 200 ? 50 : 100;
      const vmax = Math.max(step, Math.ceil(mx / step) * step);
      const tmax = Math.max(2, h.length ? h[h.length - 1].t : 2);
      const X = (t) => x0 + t / tmax * plotW;
      const Y = (v) => mid - v / vmax * plotH;
      ctx.font = "10px sans-serif";
      ctx.textAlign = "right";
      for (let val = step; val <= vmax; val += step) {
        for (const sv of [val, -val]) {
          ctx.strokeStyle = "#eef2f7";
          ctx.lineWidth = 1;
          ctx.beginPath();
          ctx.moveTo(x0, Y(sv));
          ctx.lineTo(x0 + plotW, Y(sv));
          ctx.stroke();
          ctx.fillStyle = "#94a3b8";
          ctx.fillText(String(sv), x0 - 6, Y(sv) + 3);
        }
      }
      ctx.textAlign = "left";
      ctx.strokeStyle = "#1e293b";
      ctx.fillStyle = "#1e293b";
      ctx.lineWidth = 2;
      ctx.lineJoin = "round";
      ctx.beginPath();
      ctx.moveTo(x0, 12);
      ctx.lineTo(x0, GH4 - 8);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x0, mid);
      ctx.lineTo(GW4 - 10, mid);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x0, 6);
      ctx.lineTo(x0 - 4, 14);
      ctx.lineTo(x0 + 4, 14);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(GW4 - 5, mid);
      ctx.lineTo(GW4 - 13, mid - 4);
      ctx.lineTo(GW4 - 13, mid + 4);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("v (px/s)", x0 + 4, 11);
      ctx.fillText("t (s)", GW4 - 38, mid - 6);
      ctx.fillStyle = "#64748b";
      ctx.fillText("0", x0 - 14, mid + 4);
      const line = (key, color) => {
        ctx.strokeStyle = color;
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        h.forEach((pt, i) => {
          const px2 = X(pt.t), py = Y(pt[key]);
          i === 0 ? ctx.moveTo(px2, py) : ctx.lineTo(px2, py);
        });
        ctx.stroke();
        const last = h[h.length - 1];
        if (last) {
          ctx.fillStyle = color;
          ctx.beginPath();
          ctx.arc(X(last.t), Y(last[key]), 4, 0, Math.PI * 2);
          ctx.fill();
        }
      };
      line("v1", "#2563eb");
      line("v2", "#dc2626");
      ctx.font = "bold 11px sans-serif";
      ctx.fillStyle = "#2563eb";
      ctx.fillText("\u25A0 Xe 1", GW4 - 130, 16);
      ctx.fillStyle = "#dc2626";
      ctx.fillText("\u25A0 Xe 2", GW4 - 70, 16);
    }, []);
    const render = (0, import_react6.useCallback)(() => {
      draw();
      drawGraph();
    }, [draw, drawGraph]);
    const posX = (ev) => {
      const rect = cv.current.getBoundingClientRect();
      return (ev.clientX - rect.left) * (W12 / rect.width);
    };
    const down = (ev) => {
      if (running2)
        return;
      const x = posX(ev);
      if (Math.abs(x - b1.current.x) < sizeOf(b1.current.m) / 2 + 10)
        drag.current = 1;
      else if (Math.abs(x - b2.current.x) < sizeOf(b2.current.m) / 2 + 10)
        drag.current = 2;
    };
    const move = (ev) => {
      if (!drag.current || running2)
        return;
      let x = Math.max(40, Math.min(W12 - 40, posX(ev)));
      const gap = (sizeOf(b1.current.m) + sizeOf(b2.current.m)) / 2 + 6;
      if (drag.current === 1) {
        x = Math.min(x, b2.current.x - gap);
        b1.current.x = x;
        x1Init.current = x;
      } else {
        x = Math.max(x, b1.current.x + gap);
        b2.current.x = x;
        x2Init.current = x;
      }
      force((n) => n + 1);
      render();
    };
    const up = () => {
      drag.current = null;
    };
    const tick = (0, import_react6.useCallback)((ts) => {
      if (lastRef.current == null)
        lastRef.current = ts;
      const dt = Math.min(0.05, (ts - lastRef.current) / 1e3);
      lastRef.current = ts;
      const A = b1.current, B = b2.current;
      const s1 = sizeOf(A.m), s2 = sizeOf(B.m), minGap = (s1 + s2) / 2;
      if (!collided.current && B.x - A.x <= minGap && A.v - B.v > 0) {
        collided.current = true;
        inCol.current = true;
        colT.current = 0;
        const M = A.m + B.m, a0 = A.v, b0 = B.v;
        vFrom.current = [a0, b0];
        vTo.current = [
          (A.m * a0 + B.m * b0 + B.m * e * (b0 - a0)) / M,
          (A.m * a0 + B.m * b0 + A.m * e * (a0 - b0)) / M
        ];
      }
      if (inCol.current) {
        colT.current += dt;
        const u = Math.min(1, colT.current / COL_TAU), sm = u * u * (3 - 2 * u);
        A.v = vFrom.current[0] + (vTo.current[0] - vFrom.current[0]) * sm;
        B.v = vFrom.current[1] + (vTo.current[1] - vFrom.current[1]) * sm;
        if (u >= 1)
          inCol.current = false;
      }
      A.x += A.v * dt;
      B.x += B.v * dt;
      tRef.current += dt;
      if (inCol.current && B.x - A.x < minGap) {
        const mid = (A.x + B.x) / 2;
        A.x = mid - minGap / 2;
        B.x = mid + minGap / 2;
      }
      if (A.x <= 20) {
        A.x = 20;
        A.v = 0;
      }
      if (A.x >= W12 - 20) {
        A.x = W12 - 20;
        A.v = 0;
      }
      if (B.x <= 20) {
        B.x = 20;
        B.v = 0;
      }
      if (B.x >= W12 - 20) {
        B.x = W12 - 20;
        B.v = 0;
      }
      hist.current.push({ t: tRef.current, v1: A.v, v2: B.v });
      if (hist.current.length > 4e3)
        hist.current.shift();
      force((n) => n + 1);
      render();
      if (Math.abs(A.v) < 0.5 && Math.abs(B.v) < 0.5 && collided.current && !inCol.current) {
        setRunning(false);
        return;
      }
      raf.current = requestAnimationFrame(tick);
    }, [e, render]);
    (0, import_react6.useEffect)(() => {
      if (running2) {
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        lastRef.current = null;
        raf.current = requestAnimationFrame(tick);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, tick, onComplete]);
    (0, import_react6.useEffect)(() => {
      render();
    }, [render]);
    return /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("div", { className: "sim-visuals", children: [
        /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("canvas", { ref: cv, width: W12, height: H12, onPointerDown: down, onPointerMove: move, onPointerUp: up, onPointerLeave: up, className: `w-full rounded-lg border border-gray-100 bg-slate-50 touch-none ${running2 ? "" : "cursor-grab"}` }),
        /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("canvas", { ref: gcv, width: GW4, height: GH4, className: "w-full mt-2 rounded-lg border border-gray-100 bg-slate-50" })
      ] }),
      /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("div", { className: "grid grid-cols-2 md:grid-cols-3 gap-x-4 gap-y-1 mt-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { className: "w-16 text-gray-600", children: "m\u2081" }),
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("input", { type: "range", min: 1, max: 6, value: m1, onChange: (e2) => setM1(Number(e2.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("span", { className: "w-10 text-right", children: [
              m1,
              "kg"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { className: "w-16 text-gray-600", children: "m\u2082" }),
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("input", { type: "range", min: 1, max: 6, value: m2, onChange: (e2) => setM2(Number(e2.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("span", { className: "w-10 text-right", children: [
              m2,
              "kg"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { className: "w-16 text-gray-600", children: "Lo\u1EA1i" }),
            /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("select", { value: e, onChange: (ev) => setE(Number(ev.target.value)), className: "flex-1 px-2 py-1 rounded-lg border border-gray-200 bg-white", children: [
              /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("option", { value: 1, children: "Va ch\u1EA1m \u0111\xE0n h\u1ED3i" }),
              /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("option", { value: 0, children: "Va ch\u1EA1m m\u1EC1m (d\xEDnh)" })
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { className: "w-16 text-gray-600", children: "v\u2081" }),
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("input", { type: "range", min: -100, max: 150, step: 10, value: v1, onChange: (e2) => setV1(Number(e2.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { className: "w-10 text-right", children: v1 })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { className: "w-16 text-gray-600", children: "v\u2082" }),
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("input", { type: "range", min: -150, max: 100, step: 10, value: v2, onChange: (e2) => setV2(Number(e2.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { className: "w-10 text-right", children: v2 })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("button", { onClick: () => {
            reset();
            setRunning(true);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700", "data-nb-play": "", children: [
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("button", { onClick: reset, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " \u0110\u1EB7t l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("h4", { className: "font-bold text-blue-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("p", { className: "text-[11px] text-gray-400 mt-1", children: [
            "M\u1EB9o: k\xE9o 2 xe b\u1EB1ng tay \u0111\u1EC3 s\u1EAFp x\u1EBFp v\u1ECB tr\xED ban \u0111\u1EA7u; n\xFAt ",
            /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("b", { children: "\u0110\u1EB7t l\u1EA1i" }),
            " \u0111\u01B0a xe v\u1EC1 \u0111\xFAng v\u1ECB tr\xED \u0111\xE3 s\u1EAFp."
          ] })
        ] })
      ] })
    ] });
  };
  var CollisionSimulation_default = CollisionSimulation;

  // physics/vendor/simulations/CircularMotionSimulation.tsx
  var import_react7 = __toESM(require_react());
  var import_jsx_runtime5 = __toESM(require_jsx_runtime());
  var CircularMotionSimulation = ({ title = "Chuy\u1EC3n \u0111\u1ED9ng tr\xF2n \u0111\u1EC1u", description = "V\u1EADn t\u1ED1c v lu\xF4n ti\u1EBFp tuy\u1EBFn; gia t\u1ED1c h\u01B0\u1EDBng t\xE2m a h\u01B0\u1EDBng v\xE0o t\xE2m v\u1EDBi a = \u03C9\xB2r = v\xB2/r.", onComplete }) => {
    const cv = useSimRef("CircularMotionSimulation.cv", null);
    const raf = useSimRef("CircularMotionSimulation.raf", null);
    const lastRef = useSimRef("CircularMotionSimulation.lastRef", null);
    const thetaRef = useSimRef("CircularMotionSimulation.thetaRef", 0);
    const [running2, setRunning] = useSimState("CircularMotionSimulation.running", true);
    const [omega, setOmega] = useSimState("CircularMotionSimulation.omega", 1.2);
    const [r, setR] = useSimState("CircularMotionSimulation.r", 1);
    const firstRef = useSimRef("CircularMotionSimulation.firstRef", false);
    const W12 = 460, H12 = 320;
    const cx = W12 / 2, cy = H12 / 2 - 10;
    const arrow = (ctx, x, y, dx, dy, color) => {
      const ex = x + dx, ey = y + dy, a2 = Math.atan2(dy, dx);
      ctx.strokeStyle = color;
      ctx.fillStyle = color;
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.moveTo(x, y);
      ctx.lineTo(ex, ey);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(ex, ey);
      ctx.lineTo(ex - 10 * Math.cos(a2 - 0.4), ey - 10 * Math.sin(a2 - 0.4));
      ctx.lineTo(ex - 10 * Math.cos(a2 + 0.4), ey - 10 * Math.sin(a2 + 0.4));
      ctx.closePath();
      ctx.fill();
    };
    const draw = (0, import_react7.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      const Rpix = 60 + r * 60;
      const th = thetaRef.current;
      const ox = cx + Rpix * Math.cos(th), oy = cy + Rpix * Math.sin(th);
      ctx.strokeStyle = "#cbd5e1";
      ctx.lineWidth = 1.5;
      ctx.setLineDash([5, 4]);
      ctx.beginPath();
      ctx.arc(cx, cy, Rpix, 0, Math.PI * 2);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.strokeStyle = "#94a3b8";
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(cx, cy);
      ctx.lineTo(ox, oy);
      ctx.stroke();
      ctx.fillStyle = "#334155";
      ctx.beginPath();
      ctx.arc(cx, cy, 4, 0, Math.PI * 2);
      ctx.fill();
      const vMag = omega * r * 40;
      arrow(ctx, ox, oy, -Math.sin(th) * vMag, Math.cos(th) * vMag, "#10b981");
      const aMag = Math.min(70, omega * omega * r * 22);
      arrow(ctx, ox, oy, -Math.cos(th) * aMag, -Math.sin(th) * aMag, "#ef4444");
      const g = ctx.createRadialGradient(ox - 4, oy - 4, 2, ox, oy, 12);
      g.addColorStop(0, "#bfdbfe");
      g.addColorStop(1, "#1d4ed8");
      ctx.fillStyle = g;
      ctx.beginPath();
      ctx.arc(ox, oy, 12, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = "#10b981";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("v", ox - Math.sin(th) * vMag, oy + Math.cos(th) * vMag - 6);
      ctx.fillStyle = "#ef4444";
      ctx.fillText("a", ox - Math.cos(th) * aMag - 10, oy - Math.sin(th) * aMag);
    }, [omega, r]);
    const tick = (0, import_react7.useCallback)((ts) => {
      if (lastRef.current == null)
        lastRef.current = ts;
      const dt = Math.min(0.05, (ts - lastRef.current) / 1e3);
      lastRef.current = ts;
      thetaRef.current += omega * dt;
      draw();
      raf.current = requestAnimationFrame(tick);
    }, [omega, draw]);
    (0, import_react7.useEffect)(() => {
      if (running2) {
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        lastRef.current = null;
        raf.current = requestAnimationFrame(tick);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, tick, onComplete]);
    (0, import_react7.useEffect)(() => {
      draw();
    }, [draw]);
    const T2 = (2 * Math.PI / omega).toFixed(2), v = (omega * r).toFixed(2), a = (omega * omega * r).toFixed(2);
    return /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("canvas", { ref: cv, width: W12, height: H12, className: "rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("div", { className: "flex-1", children: [
          /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("div", { className: "grid grid-cols-3 gap-2 text-center mb-3", children: [
            /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("div", { className: "p-2 rounded-lg bg-violet-50", children: [
              /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("div", { className: "font-black text-violet-700", children: [
                T2,
                "s"
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("div", { className: "text-[11px] text-gray-500", children: "Chu k\xEC T" })
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("div", { className: "p-2 rounded-lg bg-emerald-50", children: [
              /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("div", { className: "font-black text-emerald-700", children: v }),
              /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("div", { className: "text-[11px] text-gray-500", children: "T\u1ED1c \u0111\u1ED9 v=\u03C9r" })
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("div", { className: "p-2 rounded-lg bg-rose-50", children: [
              /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("div", { className: "font-black text-rose-700", children: a }),
              /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("div", { className: "text-[11px] text-gray-500", children: "a=\u03C9\xB2r" })
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("label", { className: "flex items-center gap-2 text-sm mb-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("span", { className: "w-28 text-gray-600", children: "T\u1ED1c \u0111\u1ED9 g\xF3c \u03C9" }),
            /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("input", { type: "range", min: 0.4, max: 3, step: 0.1, value: omega, onChange: (e) => setOmega(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("span", { className: "w-10 text-right font-semibold", children: omega.toFixed(1) })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("label", { className: "flex items-center gap-2 text-sm mb-3", children: [
            /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("span", { className: "w-28 text-gray-600", children: "B\xE1n k\xEDnh r" }),
            /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("input", { type: "range", min: 0.4, max: 2, step: 0.1, value: r, onChange: (e) => setR(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("span", { className: "w-10 text-right font-semibold", children: r.toFixed(1) })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("button", { onClick: () => setRunning((x) => !x), className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)(import_jsx_runtime5.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime5.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)(import_jsx_runtime5.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime5.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime5.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("h4", { className: "font-bold text-blue-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime5.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var CircularMotionSimulation_default = CircularMotionSimulation;

  // physics/vendor/simulations/MotionGraphsSimulation.tsx
  var import_react8 = __toESM(require_react());
  var import_jsx_runtime6 = __toESM(require_jsx_runtime());
  var TOPW = 660;
  var TOPH = 100;
  var GW = 660;
  var GH = 320;
  var MotionGraphsSimulation = ({ title = "\u0110\u1ED3 th\u1ECB chuy\u1EC3n \u0111\u1ED9ng", description = "Quan s\xE1t \u0111\u1ED3ng th\u1EDDi \u0111\u1ED9 d\u1ECBch chuy\u1EC3n d, v\u1EADn t\u1ED1c v, gia t\u1ED1c a theo th\u1EDDi gian. B\u1EADt/t\u1EAFt t\u1EEBng \u0111\u1ED3 th\u1ECB.", onComplete, mode = "accelerated" }) => {
    const top = useSimRef("MotionGraphsSimulation.top", null);
    const gcv = useSimRef("MotionGraphsSimulation.gcv", null);
    const raf = useSimRef("MotionGraphsSimulation.raf", null);
    const tRef = useSimRef("MotionGraphsSimulation.tRef", 0);
    const [a0, setA0] = useSimState("MotionGraphsSimulation.a0", mode === "freefall" ? 9.8 : 2);
    const [v0, setV0] = useSimState("MotionGraphsSimulation.v0", mode === "uniform" ? 5 : mode === "freefall" ? 0 : 1);
    const [showD, setShowD] = useSimState("MotionGraphsSimulation.showD", true);
    const [showV, setShowV] = useSimState("MotionGraphsSimulation.showV", mode !== "uniform");
    const [showA, setShowA] = useSimState("MotionGraphsSimulation.showA", mode !== "uniform");
    const [running2, setRunning] = useSimState("MotionGraphsSimulation.running", true);
    const a0Ref = useSimRef("MotionGraphsSimulation.a0Ref", a0), v0Ref = useSimRef("MotionGraphsSimulation.v0Ref", v0), runRef = useSimRef("MotionGraphsSimulation.runRef", running2);
    const sdRef = useSimRef("MotionGraphsSimulation.sdRef", showD), svRef = useSimRef("MotionGraphsSimulation.svRef", showV), saRef = useSimRef("MotionGraphsSimulation.saRef", showA);
    const firstRef = useSimRef("MotionGraphsSimulation.firstRef", false);
    (0, import_react8.useEffect)(() => {
      a0Ref.current = a0;
    }, [a0]);
    (0, import_react8.useEffect)(() => {
      v0Ref.current = v0;
    }, [v0]);
    (0, import_react8.useEffect)(() => {
      runRef.current = running2;
    }, [running2]);
    (0, import_react8.useEffect)(() => {
      sdRef.current = showD;
    }, [showD]);
    (0, import_react8.useEffect)(() => {
      svRef.current = showV;
    }, [showV]);
    (0, import_react8.useEffect)(() => {
      saRef.current = showA;
    }, [showA]);
    const touch = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const T2 = mode === "freefall" ? 3 : 5;
    const acc = () => mode === "uniform" ? 0 : a0Ref.current;
    const vel = (t) => v0Ref.current + acc() * t;
    const dis = (t) => v0Ref.current * t + 0.5 * acc() * t * t;
    const maxAbs = (f) => {
      let m = 0;
      for (let i = 0; i <= 40; i++) {
        const v = Math.abs(f(T2 * i / 40));
        if (v > m)
          m = v;
      }
      return m;
    };
    const niceMax = (m) => {
      m = Math.max(0.5, m);
      const p = Math.pow(10, Math.floor(Math.log10(m)));
      const n = m / p;
      const step = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10;
      return step * p;
    };
    const fmtNum = (n) => {
      const r = Math.round(n * 10) / 10;
      return Number.isInteger(r) ? String(r) : r.toFixed(1);
    };
    const drawTop = (0, import_react8.useCallback)(() => {
      const ctx = prepCanvas(top.current, TOPW, TOPH);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, TOPW, TOPH);
      const t = tRef.current, dscale = Math.max(1, maxAbs(dis));
      const frac = Math.max(-1, Math.min(1, dis(t) / dscale));
      if (mode === "freefall") {
        const x = 80, y0 = 14, y1 = TOPH - 16;
        ctx.strokeStyle = "#cbd5e1";
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(x, y0);
        ctx.lineTo(x, y1);
        ctx.stroke();
        const y = y0 + frac * (y1 - y0);
        ctx.fillStyle = "#2563eb";
        ctx.beginPath();
        ctx.arc(x, y, 11, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = "#334155";
        ctx.font = "bold 12px sans-serif";
        ctx.fillText("R\u01A1i t\u1EF1 do (g = 9,8 m/s\xB2)", 110, TOPH / 2);
      } else {
        const xL = 30, xR = TOPW - 30, y = TOPH / 2, Ox = (xL + xR) / 2, half = (xR - xL) / 2;
        ctx.fillStyle = "#e2e8f0";
        ctx.fillRect(xL, y + 14, xR - xL, 5);
        ctx.strokeStyle = "#cbd5e1";
        ctx.setLineDash([3, 3]);
        ctx.beginPath();
        ctx.moveTo(Ox, y - 24);
        ctx.lineTo(Ox, y + 24);
        ctx.stroke();
        ctx.setLineDash([]);
        ctx.fillStyle = "#94a3b8";
        ctx.font = "10px sans-serif";
        ctx.fillText("O", Ox - 3, y + 34);
        const x = Ox + frac * half * 0.9;
        ctx.fillStyle = "#2563eb";
        ctx.beginPath();
        ctx.arc(x, y, 12, 0, Math.PI * 2);
        ctx.fill();
        const vNow = vel(t), dir = vNow < 0 ? -1 : 1, vlen = Math.min(80, Math.abs(vNow) / Math.max(1, Math.abs(vel(T2)), Math.abs(v0Ref.current)) * 80);
        if (vlen > 2) {
          const ex = x + dir * vlen;
          ctx.strokeStyle = "#16a34a";
          ctx.fillStyle = "#16a34a";
          ctx.lineWidth = 3;
          ctx.lineCap = "round";
          ctx.beginPath();
          ctx.moveTo(x, y);
          ctx.lineTo(ex, y);
          ctx.stroke();
          ctx.beginPath();
          ctx.moveTo(ex, y);
          ctx.lineTo(ex - dir * 7, y - 4);
          ctx.lineTo(ex - dir * 7, y + 4);
          ctx.closePath();
          ctx.fill();
          ctx.lineCap = "butt";
          ctx.fillStyle = "#15803d";
          ctx.font = "bold italic 12px sans-serif";
          ctx.fillText("v", x + dir * vlen / 2 - 3, y - 6);
        }
      }
      ctx.fillStyle = "#64748b";
      ctx.font = "12px sans-serif";
      ctx.fillText(`t = ${t.toFixed(1)} s`, 8, 14);
    }, [mode]);
    const drawGraphs = (0, import_react8.useCallback)(() => {
      const ctx = prepCanvas(gcv.current, GW, GH);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, GW, GH);
      const t = tRef.current;
      const bands = [];
      if (sdRef.current)
        bands.push({ label: "d", color: "#2563eb", f: dis, vmax: niceMax(maxAbs(dis)), unit: "m" });
      if (svRef.current)
        bands.push({ label: "v", color: "#16a34a", f: vel, vmax: niceMax(maxAbs(vel)), unit: "m/s" });
      if (saRef.current)
        bands.push({ label: "a", color: "#dc2626", f: () => acc(), vmax: niceMax(Math.abs(acc())), unit: "m/s\xB2" });
      if (!bands.length) {
        ctx.fillStyle = "#94a3b8";
        ctx.font = "13px sans-serif";
        ctx.fillText("B\u1EADt \xEDt nh\u1EA5t m\u1ED9t \u0111\u1ED3 th\u1ECB (d, v, a)", 20, GH / 2);
        return;
      }
      const bandH = GH / bands.length, x0 = 58, x1 = GW - 14;
      bands.forEach((bnd, bi) => {
        const top0 = bi * bandH + 14, bot = (bi + 1) * bandH - 16, midY = (top0 + bot) / 2, halfH = (bot - top0) / 2;
        const X = (tt) => x0 + tt / T2 * (x1 - x0);
        const Y = (val) => midY - val / bnd.vmax * halfH * 0.92;
        ctx.font = "9px sans-serif";
        ctx.strokeStyle = "#e6ebf1";
        ctx.lineWidth = 1;
        for (let s = 1; s <= T2; s++) {
          ctx.beginPath();
          ctx.moveTo(X(s), top0);
          ctx.lineTo(X(s), bot);
          ctx.stroke();
        }
        ctx.textAlign = "right";
        for (const lv of [bnd.vmax, bnd.vmax / 2, -bnd.vmax / 2, -bnd.vmax]) {
          ctx.strokeStyle = "#e6ebf1";
          ctx.lineWidth = 1;
          ctx.beginPath();
          ctx.moveTo(x0, Y(lv));
          ctx.lineTo(x1, Y(lv));
          ctx.stroke();
          ctx.fillStyle = "#94a3b8";
          ctx.fillText(fmtNum(lv), x0 - 5, Y(lv) + 3);
        }
        ctx.textAlign = "left";
        ctx.strokeStyle = "#1e293b";
        ctx.fillStyle = "#1e293b";
        ctx.lineWidth = 1.8;
        ctx.lineJoin = "round";
        ctx.beginPath();
        ctx.moveTo(x0, top0 - 2);
        ctx.lineTo(x0, bot + 2);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(x0, midY);
        ctx.lineTo(x1, midY);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(x0, top0 - 7);
        ctx.lineTo(x0 - 4, top0 + 1);
        ctx.lineTo(x0 + 4, top0 + 1);
        ctx.closePath();
        ctx.fill();
        ctx.beginPath();
        ctx.moveTo(x1 + 5, midY);
        ctx.lineTo(x1 - 3, midY - 4);
        ctx.lineTo(x1 - 3, midY + 4);
        ctx.closePath();
        ctx.fill();
        ctx.font = "bold 12px sans-serif";
        ctx.fillStyle = bnd.color;
        ctx.fillText(`${bnd.label} (${bnd.unit})`, x0 + 4, top0 + 8);
        ctx.fillStyle = "#94a3b8";
        ctx.font = "10px sans-serif";
        ctx.fillText("t (s)", x1 - 22, midY - 5);
        ctx.fillStyle = "#64748b";
        ctx.fillText("0", x0 - 12, midY + 11);
        ctx.strokeStyle = bnd.color;
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        for (let i = 0; i <= 80; i++) {
          const tt = t * i / 80;
          const px2 = X(tt), py = Y(bnd.f(tt));
          i === 0 ? ctx.moveTo(px2, py) : ctx.lineTo(px2, py);
        }
        ctx.stroke();
        ctx.fillStyle = bnd.color;
        ctx.beginPath();
        ctx.arc(X(t), Y(bnd.f(t)), 4, 0, Math.PI * 2);
        ctx.fill();
      });
    }, [mode]);
    const tick = (0, import_react8.useCallback)(() => {
      if (runRef.current && tRef.current < T2) {
        tRef.current = Math.min(T2, tRef.current + 0.03);
        if (tRef.current >= T2)
          setRunning(false);
      }
      drawTop();
      drawGraphs();
      raf.current = requestAnimationFrame(tick);
    }, [drawTop, drawGraphs, T2]);
    (0, import_react8.useEffect)(() => {
      raf.current = requestAnimationFrame(tick);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [tick]);
    const Chk = ({ on, set, label, color }) => /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("label", { className: "flex items-center gap-1.5 cursor-pointer select-none", children: [
      /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("input", { type: "checkbox", checked: on, onChange: (e) => {
        touch();
        set(e.target.checked);
      }, className: "w-4 h-4", style: { accentColor: color } }),
      /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("span", { style: { color }, className: "font-bold", children: label })
    ] });
    return /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("div", { className: "sim-visuals", children: [
        /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("canvas", { ref: top, width: TOPW, height: TOPH, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }),
        /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("canvas", { ref: gcv, width: GW, height: GH, className: "w-full mt-2 rounded-lg border border-gray-100 bg-slate-50" })
      ] }),
      /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("div", { className: "flex flex-wrap items-center gap-x-5 gap-y-2 mt-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("span", { className: "text-gray-600", children: "Hi\u1EC7n \u0111\u1ED3 th\u1ECB:" }),
          /* @__PURE__ */ (0, import_jsx_runtime6.jsx)(Chk, { on: showD, set: setShowD, label: "d (\u0111\u1ED9 d\u1ECBch chuy\u1EC3n)", color: "#2563eb" }),
          /* @__PURE__ */ (0, import_jsx_runtime6.jsx)(Chk, { on: showV, set: setShowV, label: "v (v\u1EADn t\u1ED1c)", color: "#16a34a" }),
          /* @__PURE__ */ (0, import_jsx_runtime6.jsx)(Chk, { on: showA, set: setShowA, label: "a (gia t\u1ED1c)", color: "#dc2626" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("div", { className: "grid grid-cols-1 md:grid-cols-2 gap-x-4 gap-y-2 mt-2 text-sm", children: [
          mode !== "freefall" && /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("span", { className: "w-28 text-gray-600", children: "V\u1EADn t\u1ED1c \u0111\u1EA7u v\u2080" }),
            /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("input", { type: "range", min: 0, max: 10, step: 0.5, value: v0, onChange: (e) => {
              touch();
              setV0(Number(e.target.value));
            }, className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("span", { className: "w-14 text-right font-semibold", children: [
              v0,
              " m/s"
            ] })
          ] }),
          mode === "accelerated" && /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("span", { className: "w-28 text-gray-600", children: "Gia t\u1ED1c a" }),
            /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("input", { type: "range", min: -4, max: 4, step: 0.5, value: a0, onChange: (e) => {
              touch();
              setA0(Number(e.target.value));
            }, className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("span", { className: "w-14 text-right font-semibold", children: [
              a0,
              " m/s\xB2"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("button", { onClick: () => {
            touch();
            if (tRef.current >= T2)
              tRef.current = 0;
            setRunning((r) => !r);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)(import_jsx_runtime6.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime6.jsx)(Pause, { className: "w-4 h-4" }),
            " D\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)(import_jsx_runtime6.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime6.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("button", { onClick: () => {
            tRef.current = 0;
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime6.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " L\u1EB7p l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime6.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("h4", { className: "font-bold text-blue-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime6.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var MotionGraphsSimulation_default = MotionGraphsSimulation;

  // physics/vendor/simulations/DisplacementTimeSimulation.tsx
  var import_react9 = __toESM(require_react());
  var import_jsx_runtime7 = __toESM(require_jsx_runtime());
  var TOPW2 = 660;
  var TOPH2 = 110;
  var GW2 = 660;
  var GH2 = 250;
  var T = 10;
  var VMAX = 20;
  var DMAX = VMAX * T;
  var DisplacementTimeSimulation = ({ title = "\u0110\u1ED3 th\u1ECB \u0111\u1ED9 d\u1ECBch chuy\u1EC3n \u2013 th\u1EDDi gian", description = "Chuy\u1EC3n \u0111\u1ED9ng th\u1EB3ng \u0111\u1EC1u: \u0111\u1ED3 th\u1ECB d\u2013t l\xE0 \u0111\u01B0\u1EDDng th\u1EB3ng, \u0111\u1ED9 d\u1ED1c = v\u1EADn t\u1ED1c. \u0110\u1ED5i v\u1EADn t\u1ED1c khi \u0111ang ch\u1EA1y \u2192 \u0111\u1ED9 d\u1ED1c \u0111\u1ED3 th\u1ECB \u0111\u1ED5i theo (\u0111i\u1EC3m g\xE3y).", onComplete }) => {
    const top = useSimRef("DisplacementTimeSimulation.top", null);
    const gcv = useSimRef("DisplacementTimeSimulation.gcv", null);
    const raf = useSimRef("DisplacementTimeSimulation.raf", null);
    const tRef = useSimRef("DisplacementTimeSimulation.tRef", 0);
    const dRef = useSimRef("DisplacementTimeSimulation.dRef", 0);
    const hist = useSimRef("DisplacementTimeSimulation.hist", [{ t: 0, d: 0 }]);
    const [v, setV] = useSimState("DisplacementTimeSimulation.v", 4);
    const [running2, setRunning] = useSimState("DisplacementTimeSimulation.running", false);
    const vRef = useSimRef("DisplacementTimeSimulation.vRef", v), runRef = useSimRef("DisplacementTimeSimulation.runRef", running2);
    const firstRef = useSimRef("DisplacementTimeSimulation.firstRef", false);
    (0, import_react9.useEffect)(() => {
      vRef.current = v;
    }, [v]);
    (0, import_react9.useEffect)(() => {
      runRef.current = running2;
    }, [running2]);
    const touch = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const reset = () => {
      setRunning(false);
      tRef.current = 0;
      dRef.current = 0;
      hist.current = [{ t: 0, d: 0 }];
    };
    const drawTop = (0, import_react9.useCallback)(() => {
      const ctx = prepCanvas(top.current, TOPW2, TOPH2);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, TOPW2, TOPH2);
      const x0 = 40, x1 = TOPW2 - 40, cy = TOPH2 / 2 + 6, Ox = (x0 + x1) / 2, half = (x1 - x0) / 2;
      ctx.fillStyle = "#e2e8f0";
      ctx.fillRect(x0, cy + 18, x1 - x0, 5);
      ctx.strokeStyle = "#cbd5e1";
      ctx.setLineDash([3, 3]);
      ctx.beginPath();
      ctx.moveTo(Ox, cy - 22);
      ctx.lineTo(Ox, cy + 22);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#94a3b8";
      ctx.font = "11px sans-serif";
      ctx.fillText("O", Ox - 4, cy + 40);
      const dCl = Math.max(-DMAX, Math.min(DMAX, dRef.current));
      const cx = Ox + dCl / DMAX * half;
      const g = ctx.createLinearGradient(cx - 16, cy - 16, cx + 16, cy + 16);
      g.addColorStop(0, "#60a5fa");
      g.addColorStop(1, "#1d4ed8");
      ctx.fillStyle = g;
      ctx.fillRect(cx - 16, cy - 16, 32, 32);
      const vv = vRef.current, dir = vv < 0 ? -1 : 1, vl = Math.min(130, Math.abs(vv) * 6);
      if (vl > 2) {
        const ex = cx + dir * vl;
        ctx.strokeStyle = "#16a34a";
        ctx.fillStyle = "#16a34a";
        ctx.lineWidth = 3;
        ctx.lineCap = "round";
        ctx.beginPath();
        ctx.moveTo(cx, cy);
        ctx.lineTo(ex, cy);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(ex, cy);
        ctx.lineTo(ex - dir * 8, cy - 5);
        ctx.lineTo(ex - dir * 8, cy + 5);
        ctx.closePath();
        ctx.fill();
        ctx.lineCap = "butt";
        const mL = cx + dir * vl / 2;
        ctx.fillStyle = "#16a34a";
        ctx.font = "bold italic 13px sans-serif";
        ctx.fillText("v", mL - 3, cy - 8);
        ctx.lineWidth = 1.2;
        ctx.beginPath();
        ctx.moveTo(mL - 5, cy - 20);
        ctx.lineTo(mL + 5, cy - 20);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(mL + 5, cy - 20);
        ctx.lineTo(mL + 1, cy - 22);
        ctx.lineTo(mL + 1, cy - 18);
        ctx.closePath();
        ctx.fill();
      }
      ctx.fillStyle = "#334155";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText(`t = ${tRef.current.toFixed(1)} s \xB7 v = ${vv} m/s \xB7 d = ${dRef.current.toFixed(1)} m`, x0, 18);
    }, []);
    const drawGraph = (0, import_react9.useCallback)(() => {
      const ctx = prepCanvas(gcv.current, GW2, GH2);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, GW2, GH2);
      const x0 = 56, top0 = 12, bot = GH2 - 28, midY = (top0 + bot) / 2, plotW = GW2 - x0 - 16, halfH = (bot - top0) / 2;
      const X = (t) => x0 + t / T * plotW;
      const Y = (d) => midY - d / DMAX * halfH;
      ctx.font = "10px sans-serif";
      ctx.textAlign = "center";
      for (let i = 0; i <= T * 2; i++) {
        const t = i / 2, major = i % 2 === 0;
        ctx.strokeStyle = major ? "#dbe3ec" : "#f1f5f9";
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(X(t), top0);
        ctx.lineTo(X(t), bot);
        ctx.stroke();
        if (major && t > 0) {
          ctx.fillStyle = "#94a3b8";
          ctx.fillText(String(t), X(t), bot + 14);
        }
      }
      ctx.textAlign = "right";
      for (let d = -DMAX; d <= DMAX; d += 20) {
        if (d === 0)
          continue;
        const major = d % 40 === 0;
        ctx.strokeStyle = major ? "#dbe3ec" : "#f1f5f9";
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(x0, Y(d));
        ctx.lineTo(x0 + plotW, Y(d));
        ctx.stroke();
        if (major) {
          ctx.fillStyle = "#94a3b8";
          ctx.fillText(String(d), x0 - 6, Y(d) + 3);
        }
      }
      ctx.textAlign = "left";
      ctx.strokeStyle = "#1e293b";
      ctx.fillStyle = "#1e293b";
      ctx.lineWidth = 2;
      ctx.lineJoin = "round";
      ctx.beginPath();
      ctx.moveTo(x0, top0);
      ctx.lineTo(x0, bot);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x0, midY);
      ctx.lineTo(x0 + plotW, midY);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x0, top0 - 6);
      ctx.lineTo(x0 - 4, top0 + 2);
      ctx.lineTo(x0 + 4, top0 + 2);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(x0 + plotW + 6, midY);
      ctx.lineTo(x0 + plotW - 3, midY - 4);
      ctx.lineTo(x0 + plotW - 3, midY + 4);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("d (m)", x0 + 4, top0 + 9);
      ctx.fillText("t (s)", x0 + plotW - 4, midY - 6);
      ctx.fillStyle = "#64748b";
      ctx.fillText("0", x0 - 12, midY + 12);
      const h = hist.current;
      ctx.strokeStyle = "#2563eb";
      ctx.lineWidth = 2.8;
      ctx.beginPath();
      h.forEach((p, i) => {
        const px2 = X(p.t), py = Y(p.d);
        i === 0 ? ctx.moveTo(px2, py) : ctx.lineTo(px2, py);
      });
      ctx.stroke();
      const last = h[h.length - 1];
      if (last) {
        ctx.fillStyle = "#dc2626";
        ctx.beginPath();
        ctx.arc(X(last.t), Y(last.d), 5, 0, Math.PI * 2);
        ctx.fill();
      }
    }, []);
    const tick = (0, import_react9.useCallback)(() => {
      if (runRef.current && tRef.current < T) {
        const dt = 0.03;
        tRef.current = Math.min(T, tRef.current + dt);
        dRef.current = Math.max(-DMAX, Math.min(DMAX, dRef.current + vRef.current * dt));
        hist.current.push({ t: tRef.current, d: dRef.current });
        if (tRef.current >= T)
          setRunning(false);
      }
      drawTop();
      drawGraph();
      raf.current = requestAnimationFrame(tick);
    }, [drawTop, drawGraph]);
    (0, import_react9.useEffect)(() => {
      raf.current = requestAnimationFrame(tick);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [tick]);
    return /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)("div", { className: "sim-visuals", children: [
        /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("canvas", { ref: top, width: TOPW2, height: TOPH2, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }),
        /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("canvas", { ref: gcv, width: GW2, height: GH2, className: "w-full mt-2 rounded-lg border border-gray-100 bg-slate-50" })
      ] }),
      /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)("label", { className: "flex items-center gap-2 text-sm mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("span", { className: "w-40 text-gray-600", children: "V\u1EADn t\u1ED1c v (\u0111\u1ED5i khi \u0111ang ch\u1EA1y)" }),
          /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("input", { type: "range", min: -VMAX, max: VMAX, step: 0.5, value: v, onChange: (e) => {
            touch();
            setV(Number(e.target.value));
          }, className: "flex-1 accent-blue-500" }),
          /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)("span", { className: "w-16 text-right font-semibold", children: [
            v,
            " m/s"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("button", { onClick: () => {
            touch();
            if (tRef.current >= T)
              reset();
            setRunning((r) => !r);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)(import_jsx_runtime7.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime7.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)(import_jsx_runtime7.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime7.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)("button", { onClick: () => {
            touch();
            reset();
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime7.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " \u0110\u1EB7t l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("h4", { className: "font-bold text-blue-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "\u0110\u1ED9 d\u1ED1c c\u1EE7a \u0111\u1ED3 th\u1ECB d\u2013t b\u1EB1ng v\u1EADn t\u1ED1c. Khi \u0111ang ch\u1EA1y, k\xE9o thanh tr\u01B0\u1EE3t \u0111\u1EC3 \u0111\u1ED5i v\u1EADn t\u1ED1c \u2192 \u0111o\u1EA1n \u0111\u1ED3 th\u1ECB ti\u1EBFp theo c\xF3 \u0111\u1ED9 d\u1ED1c kh\xE1c (\u0111i\u1EC3m g\xE3y). v = 0 \u2192 \u0111o\u1EA1n n\u1EB1m ngang (v\u1EADt \u0111\u1EE9ng y\xEAn); v < 0 \u2192 v\u1EADt \u0111i ng\u01B0\u1EE3c l\u1EA1i, \u0111\u1ED9 d\u1ED1c \xE2m (d gi\u1EA3m)." })
        ] })
      ] })
    ] });
  };
  var DisplacementTimeSimulation_default = DisplacementTimeSimulation;

  // physics/vendor/simulations/InteractiveVTGraph.tsx
  var import_react11 = __toESM(require_react());

  // physics/src/question.tsx
  var import_react10 = __toESM(require_react());
  function Question(p) {
    const [answer, setAnswer] = (0, import_react10.useState)(null);
    return /* @__PURE__ */ import_react10.default.createElement("section", { className: "question" }, /* @__PURE__ */ import_react10.default.createElement("p", null, p.question), (p.options || []).map((text, i) => /* @__PURE__ */ import_react10.default.createElement("button", { key: i, onClick: () => {
      setAnswer(i);
      p.onComplete?.();
    } }, text)), answer !== null && /* @__PURE__ */ import_react10.default.createElement("p", null, answer === p.correctAnswer ? "\u0110\xFAng. " : "H\xE3y th\u1EED l\u1EA1i. ", p.explanation));
  }

  // physics/vendor/simulations/InteractiveVTGraph.tsx
  var import_jsx_runtime8 = __toESM(require_jsx_runtime());
  var InteractiveVTGraph = ({ title = "\u0110\u1ED3 th\u1ECB v-t trong chuy\u1EC3n \u0111\u1ED9ng bi\u1EBFn \u0111\u1ED5i \u0111\u1EC1u", description = "K\xE9o tr\u1EF1c ti\u1EBFp tr\xEAn \u0111\u1ED3 th\u1ECB \u0111\u1EC3 thay \u0111\u1ED5i g\xF3c nghi\xEAng (gia t\u1ED1c) ho\u1EB7c v\u1EADn t\u1ED1c ban \u0111\u1EA7u v\u2080", interactiveQuestions = [], onQuestionComplete, lessonId = "current-lesson" }) => {
    const [isPlaying, setIsPlaying] = useSimState("InteractiveVTGraph.isPlaying", false);
    const [time, setTime] = useSimState("InteractiveVTGraph.time", 0);
    const [showSettings, setShowSettings] = useSimState("InteractiveVTGraph.showSettings", true);
    const [acceleration, setAcceleration] = useSimState("InteractiveVTGraph.acceleration", 2);
    const [initialVelocity, setInitialVelocity] = useSimState("InteractiveVTGraph.initialVelocity", 5);
    const [showVectors, setShowVectors] = useSimState("InteractiveVTGraph.showVectors", true);
    const [showAngle, setShowAngle] = useSimState("InteractiveVTGraph.showAngle", true);
    const [isDragging, setIsDragging] = useSimState("InteractiveVTGraph.isDragging", false);
    const [dragMode, setDragMode] = useSimState("InteractiveVTGraph.dragMode", null);
    const canvasRef = useSimRef("InteractiveVTGraph.canvasRef", null);
    const animationFrameRef = useSimRef("InteractiveVTGraph.animationFrameRef");
    const maxTime = 10;
    const minVelocity = -10;
    const maxVelocity = 25;
    const getCurrentVelocity = (t) => {
      return initialVelocity + acceleration * t;
    };
    const drawGraph = () => {
      const canvas = canvasRef.current;
      if (!canvas)
        return;
      const ctx = canvas.getContext("2d");
      if (!ctx)
        return;
      const baseDpr = window.devicePixelRatio || 1;
      const superSamplingFactor = 2;
      const dpr = baseDpr * superSamplingFactor;
      const displayWidth = 1e3;
      const displayHeight = 600;
      const width = displayWidth * dpr;
      const height = displayHeight * dpr;
      canvas.width = width;
      canvas.height = height;
      ctx.scale(dpr, dpr);
      const workWidth = displayWidth;
      const workHeight = displayHeight;
      ctx.fillStyle = "#ffffff";
      ctx.fillRect(0, 0, workWidth, workHeight);
      const margin = 70;
      const graphWidth = workWidth - 2 * margin;
      const graphHeight = workHeight - 2 * margin;
      const originX = margin;
      const originY = workHeight - margin;
      const vRange = maxVelocity - minVelocity;
      const yZero = originY - (0 - minVelocity) / vRange * graphHeight;
      ctx.imageSmoothingEnabled = true;
      ctx.imageSmoothingQuality = "high";
      ctx.textRendering = "optimizeLegibility";
      ctx.strokeStyle = "#000000";
      ctx.lineWidth = 3.5;
      ctx.lineCap = "square";
      ctx.beginPath();
      ctx.moveTo(originX, yZero);
      ctx.lineTo(originX + graphWidth, yZero);
      ctx.moveTo(originX, originY);
      ctx.lineTo(originX, originY - graphHeight);
      ctx.stroke();
      const arrowSizeT = 14;
      const arrowEndT = originX + graphWidth;
      ctx.fillStyle = "#000000";
      ctx.beginPath();
      ctx.moveTo(arrowEndT, yZero);
      ctx.lineTo(arrowEndT - arrowSizeT, yZero - arrowSizeT / 2);
      ctx.lineTo(arrowEndT - arrowSizeT, yZero + arrowSizeT / 2);
      ctx.closePath();
      ctx.fill();
      const arrowSizeV = 14;
      const arrowEndV = originY - graphHeight;
      ctx.beginPath();
      ctx.moveTo(originX, arrowEndV);
      ctx.lineTo(originX - arrowSizeV / 2, arrowEndV + arrowSizeV);
      ctx.lineTo(originX + arrowSizeV / 2, arrowEndV + arrowSizeV);
      ctx.closePath();
      ctx.fill();
      ctx.fillStyle = "#000000";
      ctx.font = 'bold 26px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';
      ctx.textAlign = "right";
      ctx.fillText("t (s)", originX + graphWidth - 5, yZero - 15);
      ctx.textAlign = "center";
      ctx.fillText("v (m/s)", originX + 50, originY - graphHeight - 10);
      ctx.strokeStyle = "#d1d5db";
      ctx.lineWidth = 1.5;
      ctx.fillStyle = "#000000";
      ctx.font = 'bold 22px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';
      for (let t = 0; t <= maxTime; t += 2) {
        if (t === 0)
          continue;
        const x = originX + t / maxTime * graphWidth;
        ctx.beginPath();
        ctx.moveTo(x, originY);
        ctx.lineTo(x, originY - graphHeight);
        ctx.stroke();
        ctx.textAlign = "center";
        ctx.fillText(t.toString(), x, originY + 30);
      }
      for (let v = minVelocity; v <= maxVelocity; v += 5) {
        if (v === 0)
          continue;
        const y = originY - (v - minVelocity) / vRange * graphHeight;
        ctx.beginPath();
        ctx.moveTo(originX, y);
        ctx.lineTo(originX + graphWidth, y);
        ctx.stroke();
        ctx.textAlign = "right";
        ctx.fillText(v.toString(), originX - 15, y + 8);
      }
      ctx.strokeStyle = "#2563eb";
      ctx.lineWidth = 5;
      ctx.lineCap = "round";
      ctx.lineJoin = "round";
      ctx.shadowColor = "rgba(37, 99, 235, 0.3)";
      ctx.shadowBlur = 2;
      ctx.beginPath();
      for (let t = 0; t <= maxTime; t += 0.05) {
        const v = getCurrentVelocity(t);
        const x = originX + t / maxTime * graphWidth;
        const y = originY - (v - minVelocity) / vRange * graphHeight;
        if (t === 0) {
          ctx.moveTo(x, y);
        } else {
          ctx.lineTo(x, y);
        }
      }
      ctx.stroke();
      ctx.shadowColor = "transparent";
      ctx.shadowBlur = 0;
      const currentV = getCurrentVelocity(time);
      const currentX = originX + time / maxTime * graphWidth;
      const currentY = originY - (currentV - minVelocity) / vRange * graphHeight;
      ctx.strokeStyle = "#dc2626";
      ctx.lineWidth = 3;
      ctx.fillStyle = "#ef4444";
      ctx.beginPath();
      ctx.arc(currentX, currentY, 10, 0, 2 * Math.PI);
      ctx.fill();
      ctx.stroke();
      ctx.strokeStyle = "#ffffff";
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(currentX, currentY, 10, 0, 2 * Math.PI);
      ctx.stroke();
      if (showAngle) {
        const t0 = 0;
        const v0 = initialVelocity;
        const x0 = originX;
        const y0 = originY - (v0 - minVelocity) / vRange * graphHeight;
        ctx.strokeStyle = "#dc2626";
        ctx.lineWidth = 3;
        ctx.setLineDash([10, 10]);
        ctx.beginPath();
        ctx.moveTo(x0, y0);
        ctx.lineTo(x0 + 200, y0);
        ctx.stroke();
        ctx.setLineDash([]);
        const t1 = 2;
        const v1 = getCurrentVelocity(t1);
        const x1 = originX + t1 / maxTime * graphWidth;
        const y1 = originY - (v1 - minVelocity) / vRange * graphHeight;
        const deltaY = y0 - y1;
        const deltaX = x1 - x0;
        const angleAlpha = Math.atan(Math.abs(deltaY) / deltaX);
        const arcRadius = 70;
        ctx.strokeStyle = "#dc2626";
        ctx.lineWidth = 3.5;
        ctx.beginPath();
        if (acceleration >= 0) {
          ctx.arc(x0, y0, arcRadius, 0, -angleAlpha, true);
        } else {
          ctx.arc(x0, y0, arcRadius, 0, angleAlpha, false);
        }
        ctx.stroke();
        ctx.fillStyle = "#dc2626";
        ctx.font = 'bold 32px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
        ctx.textAlign = "center";
        const labelAngleX = x0 + arcRadius * 0.7;
        const labelAngleY = acceleration >= 0 ? y0 - arcRadius * 0.25 : y0 + arcRadius * 0.35;
        ctx.fillText("\u03B1", labelAngleX, labelAngleY);
      } else if (showVectors) {
        const t1 = Math.max(time, 2);
        const t2 = t1 + 1;
        const v1 = getCurrentVelocity(t1);
        const v2 = getCurrentVelocity(t2);
        const x1 = originX + t1 / maxTime * graphWidth;
        const y1 = originY - (v1 - minVelocity) / vRange * graphHeight;
        const x2 = originX + t2 / maxTime * graphWidth;
        const y2 = originY - (v2 - minVelocity) / vRange * graphHeight;
        ctx.strokeStyle = "#059669";
        ctx.lineWidth = 3.5;
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        ctx.beginPath();
        ctx.moveTo(x1, y1);
        ctx.lineTo(x2, y1);
        ctx.lineTo(x2, y2);
        ctx.stroke();
        ctx.fillStyle = "#059669";
        ctx.font = 'bold 22px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
        ctx.textAlign = "center";
        ctx.fillText("\u0394t = 1s", (x1 + x2) / 2, y1 + 32);
        ctx.textAlign = "left";
        ctx.fillText(`\u0394v = ${(v2 - v1).toFixed(1)} m/s`, x2 + 12, (y1 + y2) / 2);
      }
      ctx.font = 'bold 26px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
      ctx.fillStyle = "#000000";
      ctx.textAlign = "center";
      const centerX = originX + graphWidth / 2;
      const topY = 50;
      ctx.fillText(`v = v\u2080 + at`, centerX, topY);
      ctx.fillText(`v = ${initialVelocity} + ${acceleration} \xD7 t`, centerX, topY + 40);
    };
    (0, import_react11.useEffect)(() => {
      if (isPlaying) {
        const animate = () => {
          setTime((prevTime) => {
            const newTime = prevTime + 0.05;
            if (newTime >= maxTime) {
              setIsPlaying(false);
              return maxTime;
            }
            return newTime;
          });
          animationFrameRef.current = requestAnimationFrame(animate);
        };
        animationFrameRef.current = requestAnimationFrame(animate);
      }
      return () => {
        if (animationFrameRef.current) {
          cancelAnimationFrame(animationFrameRef.current);
        }
      };
    }, [isPlaying]);
    (0, import_react11.useEffect)(() => {
      drawGraph();
    }, [time, acceleration, initialVelocity, showVectors, showAngle]);
    const handleReset = () => {
      setIsPlaying(false);
      setTime(0);
    };
    const handleAccelerationChange = (value) => {
      setAcceleration(value);
      if (time > 0) {
        setTime(0);
      }
    };
    const getCanvasCoordinates = (e) => {
      const canvas = canvasRef.current;
      if (!canvas)
        return null;
      const rect = canvas.getBoundingClientRect();
      let clientX, clientY;
      if ("touches" in e) {
        clientX = e.touches[0].clientX;
        clientY = e.touches[0].clientY;
      } else {
        clientX = e.clientX;
        clientY = e.clientY;
      }
      const dpr = (window.devicePixelRatio || 1) * 2;
      const x = (clientX - rect.left) * dpr;
      const y = (clientY - rect.top) * dpr;
      return { x, y };
    };
    const handleCanvasMouseDown = (e) => {
      const coords = getCanvasCoordinates(e);
      if (!coords)
        return;
      const canvas = canvasRef.current;
      if (!canvas)
        return;
      const dpr = (window.devicePixelRatio || 1) * 2;
      const workWidth = 1e3;
      const workHeight = 600;
      const margin = 70;
      const originX = margin;
      const originY = workHeight - margin;
      const logicX = coords.x / dpr;
      const logicY = coords.y / dpr;
      if (logicX < originX + 50) {
        setDragMode("velocity");
        setIsDragging(true);
      } else {
        setDragMode("angle");
        setIsDragging(true);
      }
    };
    const handleCanvasMouseMove = (e) => {
      if (!isDragging)
        return;
      const coords = getCanvasCoordinates(e);
      if (!coords)
        return;
      const canvas = canvasRef.current;
      if (!canvas)
        return;
      const dpr = (window.devicePixelRatio || 1) * 2;
      const workWidth = 1e3;
      const workHeight = 600;
      const margin = 70;
      const graphWidth = workWidth - 2 * margin;
      const graphHeight = workHeight - 2 * margin;
      const originX = margin;
      const originY = workHeight - margin;
      const minVelocity2 = -10;
      const maxVelocity2 = 25;
      const logicX = coords.x / dpr;
      const logicY = coords.y / dpr;
      if (dragMode === "velocity") {
        const vRange = maxVelocity2 - minVelocity2;
        const newV = minVelocity2 + (originY - logicY) / graphHeight * vRange;
        const clampedV = Math.max(-10, Math.min(15, newV));
        setInitialVelocity(parseFloat(clampedV.toFixed(1)));
      } else if (dragMode === "angle") {
        const deltaX = logicX - originX;
        const deltaY = originY - logicY;
        if (deltaX > 50) {
          const vRange = maxVelocity2 - minVelocity2;
          const v0Y = originY - (initialVelocity - minVelocity2) / vRange * graphHeight;
          const slope = (v0Y - logicY) / deltaX;
          const newA = slope * (graphWidth / maxTime) * vRange / graphHeight;
          const clampedA = Math.max(-5, Math.min(5, newA));
          setAcceleration(parseFloat(clampedA.toFixed(1)));
        }
      }
    };
    const handleCanvasMouseUp = () => {
      setIsDragging(false);
      setDragMode(null);
    };
    const handleCanvasMouseEnter = () => {
    };
    const handleCanvasMouseLeave = () => {
      setIsDragging(false);
      setDragMode(null);
    };
    const handleCanvasTouchStart = (e) => {
      e.preventDefault();
      handleCanvasMouseDown(e);
    };
    const handleCanvasTouchMove = (e) => {
      e.preventDefault();
      handleCanvasMouseMove(e);
    };
    const handleCanvasTouchEnd = () => {
      handleCanvasMouseUp();
    };
    return /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("style", { children: `
        input[type="range"].interactive-slider {
          -webkit-appearance: none;
          appearance: none;
          background: linear-gradient(to right, #93c5fd 0%, #3b82f6 50%, #1d4ed8 100%);
          border-radius: 6px;
          outline: none;
          height: 8px;
        }
        input[type="range"].interactive-slider::-webkit-slider-thumb {
          -webkit-appearance: none;
          appearance: none;
          width: 24px;
          height: 24px;
          background: #3b82f6;
          border: 3px solid white;
          border-radius: 50%;
          cursor: grab;
          box-shadow: 0 2px 6px rgba(0,0,0,0.3);
        }
        input[type="range"].interactive-slider::-webkit-slider-thumb:active {
          cursor: grabbing;
          background: #1d4ed8;
          transform: scale(1.1);
        }
        input[type="range"].interactive-slider::-moz-range-thumb {
          width: 24px;
          height: 24px;
          background: #3b82f6;
          border: 3px solid white;
          border-radius: 50%;
          cursor: grab;
          box-shadow: 0 2px 6px rgba(0,0,0,0.3);
        }
        input[type="range"].interactive-slider::-moz-range-thumb:active {
          cursor: grabbing;
          background: #1d4ed8;
          transform: scale(1.1);
        }
        
        .canvas-interactive {
          cursor: grab;
        }
        
        .canvas-interactive.dragging {
          cursor: grabbing;
        }
      ` }),
      /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("div", { className: "mb-4 bg-blue-50 rounded-lg p-6 flex justify-center border-2 border-blue-200", children: /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("canvas", { ref: canvasRef, "data-logical-width": "1000", "data-logical-height": "600", style: {
        width: "1000px",
        height: "600px",
        maxWidth: "100%",
        imageRendering: "crisp-edges",
        touchAction: "none"
      }, className: `border-2 border-blue-400 rounded-lg shadow-lg transition-all hover:shadow-xl hover:border-blue-500 canvas-interactive ${isDragging ? "dragging" : ""}`, onMouseDown: handleCanvasMouseDown, onMouseMove: handleCanvasMouseMove, onMouseUp: handleCanvasMouseUp, onMouseEnter: handleCanvasMouseEnter, onMouseLeave: handleCanvasMouseLeave, onTouchStart: handleCanvasTouchStart, onTouchMove: handleCanvasTouchMove, onTouchEnd: handleCanvasTouchEnd }) }) }),
      /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("div", { className: "mb-4 bg-gradient-to-r from-orange-50 to-yellow-50 border-2 border-orange-300 rounded-lg p-4", children: /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "flex items-center gap-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("div", { className: "text-5xl", style: { color: "#f59e0b", textShadow: "2px 2px 4px rgba(0,0,0,0.2)" }, children: "\u{1F446}" }),
          /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "flex-1", children: [
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("p", { className: "text-sm font-semibold text-orange-900", children: "T\u01B0\u01A1ng t\xE1c tr\u1EF1c ti\u1EBFp tr\xEAn \u0111\u1ED3 th\u1ECB!" }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("p", { className: "text-xs text-orange-800 mt-1", children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "K\xE9o tr\xEAn \u0111\u1ED3 th\u1ECB:" }),
              " Thay \u0111\u1ED5i g\xF3c \u03B1 (gia t\u1ED1c) \u2022",
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: " K\xE9o g\u1EA7n tr\u1EE5c v:" }),
              " Thay \u0111\u1ED5i v\u2080"
            ] })
          ] })
        ] }) }),
        /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "space-y-4", children: [
          /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "flex items-center gap-3", children: [
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("button", { onClick: () => setIsPlaying(!isPlaying), className: "btn-primary px-4 py-2 rounded-lg flex items-center gap-2", "data-nb-play": "", children: isPlaying ? /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)(import_jsx_runtime8.Fragment, { children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)(Pause, { className: "w-4 h-4" }),
              "T\u1EA1m d\u1EEBng"
            ] }) : /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)(import_jsx_runtime8.Fragment, { children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)(Play, { className: "w-4 h-4" }),
              "Ch\u1EA1y"
            ] }) }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("button", { onClick: handleReset, className: "btn-secondary px-4 py-2 rounded-lg flex items-center gap-2", children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)(RotateCcw, { className: "w-4 h-4" }),
              "\u0110\u1EB7t l\u1EA1i"
            ] })
          ] }),
          showSettings && /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "bg-gradient-to-r from-blue-50 to-indigo-50 border-2 border-blue-300 rounded-lg p-5 space-y-5 shadow-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "flex items-center gap-2 text-blue-900 font-semibold mb-3", children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { className: "text-2xl", style: { filter: "hue-rotate(40deg) saturate(2)" }, children: "\u{1F39A}\uFE0F" }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { children: "\u0110i\u1EC1u ch\u1EC9nh tham s\u1ED1:" })
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("label", { className: "block text-base font-bold text-gray-800 mb-3 flex items-center gap-2", children: [
                /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("svg", { width: "28", height: "28", viewBox: "0 0 24 24", fill: "#f59e0b", className: "flex-shrink-0", children: [
                  /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("path", { d: "M9 11H7v2h2v-2zm4 0h-2v2h2v-2zm4 0h-2v2h2v-2zm2-7h-1V2h-2v2H8V2H6v2H5c-1.11 0-1.99.9-1.99 2L3 20c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V9h14v11z" }),
                  /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("circle", { cx: "12", cy: "16", r: "1.5", fill: "#f59e0b" })
                ] }),
                "Gia t\u1ED1c (a): ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("span", { className: "text-blue-600 text-lg", children: [
                  acceleration,
                  " m/s\xB2"
                ] })
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("input", { type: "range", min: "-5", max: "5", step: "0.5", value: acceleration, onChange: (e) => handleAccelerationChange(parseFloat(e.target.value)), className: "w-full interactive-slider" }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "flex justify-between text-xs font-medium text-gray-600 mt-2", children: [
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { children: "-5" }),
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { children: "0" }),
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { children: "+5" })
              ] })
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("label", { className: "block text-base font-bold text-gray-800 mb-3 flex items-center gap-2", children: [
                /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("svg", { width: "28", height: "28", viewBox: "0 0 24 24", fill: "#f59e0b", className: "flex-shrink-0", children: [
                  /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("path", { d: "M9 11H7v2h2v-2zm4 0h-2v2h2v-2zm4 0h-2v2h2v-2zm2-7h-1V2h-2v2H8V2H6v2H5c-1.11 0-1.99.9-1.99 2L3 20c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V9h14v11z" }),
                  /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("circle", { cx: "12", cy: "16", r: "1.5", fill: "#f59e0b" })
                ] }),
                "V\u1EADn t\u1ED1c ban \u0111\u1EA7u (v\u2080): ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("span", { className: "text-blue-600 text-lg", children: [
                  initialVelocity,
                  " m/s"
                ] })
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("input", { type: "range", min: "-10", max: "15", step: "0.5", value: initialVelocity, onChange: (e) => setInitialVelocity(parseFloat(e.target.value)), className: "w-full interactive-slider" }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "flex justify-between text-xs font-medium text-gray-600 mt-2", children: [
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { children: "-10" }),
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { children: "0" }),
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { children: "+15" })
              ] })
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "mb-4", children: [
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("h3", { className: "text-lg font-bold text-gray-800 mb-2", children: title }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("p", { className: "text-sm text-gray-600", children: description }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("p", { className: "text-sm text-blue-600 font-medium mt-1", children: "\u{1F4A1} K\xE9o tr\u1EF1c ti\u1EBFp tr\xEAn \u0111\u1ED3 th\u1ECB \u0111\u1EC3 thay \u0111\u1ED5i g\xF3c \u03B1 ho\u1EB7c v\u2080" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "mt-4 bg-blue-50 border-2 border-blue-300 rounded-lg p-4", children: [
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("h4", { className: "text-sm font-semibold text-blue-900 mb-2", children: "\u{1F5B1}\uFE0F C\xE1ch t\u01B0\u01A1ng t\xE1c:" }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("ul", { className: "text-sm text-blue-800 space-y-1", children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("li", { children: [
                "\u2022 ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "K\xE9o tr\xEAn \u0111\u1ED3 th\u1ECB" }),
                " (v\xF9ng gi\u1EEFa) \u2192 Thay \u0111\u1ED5i ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "g\xF3c nghi\xEAng" }),
                " (gia t\u1ED1c a)"
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("li", { children: [
                "\u2022 ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "K\xE9o g\u1EA7n tr\u1EE5c v" }),
                " (b\xEAn tr\xE1i) \u2192 Thay \u0111\u1ED5i ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "v\u1EADn t\u1ED1c ban \u0111\u1EA7u v\u2080" })
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("li", { children: [
                "\u2022 Tr\xEAn ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "\u0111i\u1EC7n tho\u1EA1i" }),
                ": D\xF9ng ng\xF3n tay k\xE9o tr\u1EF1c ti\u1EBFp tr\xEAn \u0111\u1ED3 th\u1ECB"
              ] })
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "mt-4 bg-yellow-50 border border-yellow-200 rounded-lg p-4", children: [
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("h4", { className: "text-sm font-semibold text-gray-800 mb-2", children: "\u{1F4A1} Quan s\xE1t:" }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("ul", { className: "text-sm text-gray-700 space-y-1", children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("li", { children: [
                "\u2022 ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "H\u1EC7 s\u1ED1 g\xF3c" }),
                " c\u1EE7a \u0111\u01B0\u1EDDng th\u1EB3ng ch\xEDnh l\xE0 ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "gia t\u1ED1c (a)" })
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("li", { children: [
                "\u2022 a ",
                ">",
                " 0: \u0110\u01B0\u1EDDng nghi\xEAng l\xEAn \u2192 ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "chuy\u1EC3n \u0111\u1ED9ng nhanh d\u1EA7n" })
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("li", { children: [
                "\u2022 a ",
                "<",
                " 0: \u0110\u01B0\u1EDDng nghi\xEAng xu\u1ED1ng \u2192 ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "chuy\u1EC3n \u0111\u1ED9ng ch\u1EADm d\u1EA7n" })
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("li", { children: [
                "\u2022 a = 0: \u0110\u01B0\u1EDDng n\u1EB1m ngang \u2192 ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "chuy\u1EC3n \u0111\u1ED9ng \u0111\u1EC1u" })
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("li", { children: [
                "\u2022 ",
                /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("strong", { children: "G\xF3c \u03B1" }),
                " (\u0111\u1ECF): G\xF3c nghi\xEAng c\u1EE7a \u0111\u01B0\u1EDDng v-t. G\xF3c c\xE0ng l\u1EDBn \u2192 \u0111\u01B0\u1EDDng c\xE0ng d\u1ED1c \u2192 gia t\u1ED1c c\xE0ng l\u1EDBn"
              ] })
            ] })
          ] }),
          interactiveQuestions && interactiveQuestions.length > 0 && /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "mt-6 space-y-4", children: [
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("h4", { className: "text-lg font-bold text-gray-800", children: "\u{1F3AF} C\xE2u h\u1ECFi t\u01B0\u01A1ng t\xE1c" }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("p", { className: "text-sm text-gray-600 mb-4", children: "H\xE3y th\u1EED nghi\u1EC7m v\u1EDBi c\xE1c thanh tr\u01B0\u1EE3t \u1EDF tr\xEAn, sau \u0111\xF3 tr\u1EA3 l\u1EDDi c\xE1c c\xE2u h\u1ECFi d\u01B0\u1EDBi \u0111\xE2y:" }),
            interactiveQuestions.map((q, index) => /* @__PURE__ */ (0, import_jsx_runtime8.jsx)(Question, { question: q.question, options: q.options, correctAnswer: q.correctAnswer, explanation: q.explanation, hint: q.hint, difficulty: "easy", lessonId, onAnswer: (isCorrect) => {
              if (isCorrect && onQuestionComplete) {
                onQuestionComplete();
              }
            } }, q.id || `question-${index}`))
          ] })
        ] })
      ] })
    ] });
  };
  var InteractiveVTGraph_default = InteractiveVTGraph;

  // physics/vendor/simulations/HookeSpringSimulation.tsx
  var import_react12 = __toESM(require_react());
  var import_jsx_runtime9 = __toESM(require_jsx_runtime());
  var HookeSpringSimulation = ({ title = "\u0110\u1ECBnh lu\u1EADt Hooke \u2014 F = k\xB7\u0394l", description = "L\u1EF1c \u0111\xE0n h\u1ED3i t\u1EC9 l\u1EC7 v\u1EDBi \u0111\u1ED9 bi\u1EBFn d\u1EA1ng. \u0110\u1ED3 th\u1ECB F\u2013\u0394l l\xE0 \u0111\u01B0\u1EDDng th\u1EB3ng; \u0111\u1ED9 d\u1ED1c ch\xEDnh l\xE0 \u0111\u1ED9 c\u1EE9ng k.", onComplete }) => {
    const cv = useSimRef("HookeSpringSimulation.cv", null);
    const [k, setK] = useSimState("HookeSpringSimulation.k", 20);
    const [dl, setDl] = useSimState("HookeSpringSimulation.dl", 0.1);
    const firstRef = useSimRef("HookeSpringSimulation.firstRef", false);
    const F = k * dl;
    const W12 = 620, H12 = 280;
    const draw = (0, import_react12.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      const sx = 90, top = 30, natural = 90, stretch = dl * 600;
      ctx.fillStyle = "#64748b";
      ctx.fillRect(sx - 30, top - 8, 60, 8);
      ctx.strokeStyle = "#94a3b8";
      ctx.lineWidth = 2.5;
      ctx.beginPath();
      const len = natural + stretch, coils = 12;
      for (let i = 0; i <= coils; i++) {
        const yy = top + len * (i / coils);
        const xx = sx + (i % 2 === 0 ? -14 : 14);
        i === 0 ? ctx.moveTo(sx, top) : ctx.lineTo(xx, yy);
      }
      ctx.lineTo(sx, top + len);
      ctx.stroke();
      const my = top + len;
      const g = ctx.createLinearGradient(0, my, 0, my + 44);
      g.addColorStop(0, "#fcd34d");
      g.addColorStop(1, "#d97706");
      ctx.fillStyle = g;
      ctx.fillRect(sx - 26, my, 52, 44);
      ctx.strokeStyle = "#92400e";
      ctx.strokeRect(sx - 26, my, 52, 44);
      ctx.strokeStyle = "#ef4444";
      ctx.setLineDash([4, 3]);
      ctx.beginPath();
      ctx.moveTo(sx + 50, top + natural);
      ctx.lineTo(sx + 50, my);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#dc2626";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText(`\u0394l=${(dl * 100).toFixed(0)}cm`, sx + 56, (top + natural + my) / 2);
      if (Math.abs(F) > 0.05) {
        const ax = sx - 58, ay = my + 22, len2 = Math.min(150, Math.abs(F) * 5), dir = F > 0 ? -1 : 1;
        const ey = ay + dir * len2;
        ctx.strokeStyle = "#16a34a";
        ctx.fillStyle = "#16a34a";
        ctx.lineWidth = 3;
        ctx.beginPath();
        ctx.moveTo(ax, ay);
        ctx.lineTo(ax, ey);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(ax, ey);
        ctx.lineTo(ax - 5, ey - dir * 9);
        ctx.lineTo(ax + 5, ey - dir * 9);
        ctx.closePath();
        ctx.fill();
        ctx.font = "bold 12px sans-serif";
        ctx.fillText("F\u0111h", ax - 22, dir < 0 ? ey - 4 : ey + 14);
      }
      const gx0 = 305, gx1 = W12 - 16, gyTop = 16, gyBot = H12 - 22;
      const DLMIN = -0.1, DLMAX = 0.3, FMINg = -5, FMAXg = 15;
      const GX = (d) => gx0 + (d - DLMIN) / (DLMAX - DLMIN) * (gx1 - gx0);
      const GY = (f) => gyBot - (f - FMINg) / (FMAXg - FMINg) * (gyBot - gyTop);
      const ox = GX(0), oy = GY(0);
      ctx.strokeStyle = "#eef2f7";
      ctx.lineWidth = 1;
      for (let d = -0.1; d <= 0.3001; d += 0.1) {
        ctx.beginPath();
        ctx.moveTo(GX(d), gyTop);
        ctx.lineTo(GX(d), gyBot);
        ctx.stroke();
      }
      ctx.strokeStyle = "#94a3b8";
      ctx.fillStyle = "#94a3b8";
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(ox, gyTop);
      ctx.lineTo(ox, gyBot);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(gx0, oy);
      ctx.lineTo(gx1, oy);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(ox, gyTop);
      ctx.lineTo(ox - 4, gyTop + 8);
      ctx.lineTo(ox + 4, gyTop + 8);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(gx1, oy);
      ctx.lineTo(gx1 - 8, oy - 4);
      ctx.lineTo(gx1 - 8, oy + 4);
      ctx.closePath();
      ctx.fill();
      ctx.fillStyle = "#475569";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("F(N)", ox + 5, gyTop + 10);
      ctx.fillText("\u0394l(m)", gx1 - 38, oy - 6);
      const fLo = Math.max(FMINg, Math.min(FMAXg, k * DLMIN)), fHi = Math.max(FMINg, Math.min(FMAXg, k * DLMAX));
      ctx.strokeStyle = "#2563eb";
      ctx.lineWidth = 3.5;
      ctx.beginPath();
      ctx.moveTo(GX(DLMIN), GY(fLo));
      ctx.lineTo(GX(DLMAX), GY(fHi));
      ctx.stroke();
      const px2 = GX(Math.max(DLMIN, Math.min(DLMAX, dl))), py = GY(Math.max(FMINg, Math.min(FMAXg, F)));
      ctx.strokeStyle = "rgba(239,68,68,0.5)";
      ctx.setLineDash([3, 3]);
      ctx.beginPath();
      ctx.moveTo(px2, oy);
      ctx.lineTo(px2, py);
      ctx.moveTo(ox, py);
      ctx.lineTo(px2, py);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#ef4444";
      ctx.beginPath();
      ctx.arc(px2, py, 6, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = "#fff";
      ctx.lineWidth = 2;
      ctx.stroke();
    }, [k, dl]);
    (0, import_react12.useEffect)(() => {
      draw();
    }, [draw]);
    const dragRef = useSimRef("HookeSpringSimulation.dragRef", false);
    const onDown = (e) => {
      const r = cv.current.getBoundingClientRect();
      const mx = (e.clientX - r.left) / r.width * W12, my = (e.clientY - r.top) / r.height * H12;
      const massY = 120 + dl * 600 + 22;
      if (Math.abs(mx - 90) < 40 && Math.abs(my - massY) < 36) {
        dragRef.current = true;
        firstRef.current || (firstRef.current = true, onComplete?.());
        e.target.setPointerCapture?.(e.pointerId);
      }
    };
    const onMove = (e) => {
      if (!dragRef.current)
        return;
      const r = cv.current.getBoundingClientRect();
      const my = (e.clientY - r.top) / r.height * H12;
      setDl(Math.max(-0.1, Math.min(0.3, (my - 22 - 120) / 600)));
    };
    const onUp = () => {
      dragRef.current = false;
    };
    return /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("canvas", { ref: cv, width: W12, height: H12, onPointerDown: onDown, onPointerMove: onMove, onPointerUp: onUp, onPointerLeave: onUp, className: "w-full rounded-lg border border-gray-100 bg-slate-50 touch-none cursor-grab active:cursor-grabbing" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("div", { className: "text-center p-2 rounded-xl bg-amber-50 my-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("span", { className: "font-black text-amber-700 text-lg", children: [
            "F = ",
            F.toFixed(1),
            " N"
          ] }),
          " ",
          /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("span", { className: "text-xs text-gray-500", children: [
            "= k\xB7\u0394l = ",
            k,
            " \xD7 ",
            dl.toFixed(2)
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("div", { className: "grid grid-cols-2 gap-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("span", { className: "w-24 text-gray-600", children: "\u0110\u1ED9 c\u1EE9ng k" }),
            /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("input", { type: "range", min: 5, max: 50, step: 5, value: k, onChange: (e) => {
              firstRef.current || (firstRef.current = true, onComplete?.());
              setK(Number(e.target.value));
            }, className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("span", { className: "w-16 text-right font-semibold", children: [
              k,
              " N/m"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("span", { className: "w-24 text-gray-600", children: "\u0110\u1ED9 bi\u1EBFn d\u1EA1ng \u0394l" }),
            /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("input", { type: "range", min: -0.1, max: 0.3, step: 0.01, value: dl, onChange: (e) => {
              firstRef.current || (firstRef.current = true, onComplete?.());
              setDl(Number(e.target.value));
            }, className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("span", { className: "w-16 text-right font-semibold", children: [
              (dl * 100).toFixed(0),
              " cm"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("h4", { className: "font-bold text-amber-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var HookeSpringSimulation_default = HookeSpringSimulation;

  // physics/vendor/simulations/SpringOscillatorSimulation.tsx
  var import_react13 = __toESM(require_react());
  var import_jsx_runtime10 = __toESM(require_jsx_runtime());
  var G2 = 9.8;
  var W3 = 660;
  var H3 = 384;
  var EQX = 360;
  var CY = 76;
  var WALLX = 44;
  var SCALE2 = 600;
  var XMAX = 0.25;
  var WIN = 8;
  var SpringOscillatorSimulation = ({ title = "Con l\u1EAFc l\xF2 xo \u2014 k\xE9o & th\u1EA3 (t\u1EAFt d\u1EA7n)", description = "K\xE9o v\u1EADt t\u1EDBi v\u1ECB tr\xED x\u2080 r\u1ED3i th\u1EA3: bi\xEAn \u0111\u1ED9 b\u1EB1ng x\u2080. C\u1EA5p v\u1EADn t\u1ED1c \u0111\u1EA7u v\u2080 \u2192 bi\xEAn \u0111\u1ED9 l\u1EDBn h\u01A1n. T\u0103ng h\u1EC7 s\u1ED1 ma s\xE1t/c\u1EA3n \u2192 dao \u0111\u1ED9ng t\u1EAFt d\u1EA7n.", onComplete }) => {
    const cv = useSimRef("SpringOscillatorSimulation.cv", null);
    const [k, setK] = useSimState("SpringOscillatorSimulation.k", 20);
    const [m, setM] = useSimState("SpringOscillatorSimulation.m", 1);
    const [mu, setMu] = useSimState("SpringOscillatorSimulation.mu", 0);
    const [v0, setV0] = useSimState("SpringOscillatorSimulation.v0", 0);
    const [showForce, setShowForce] = useSimState("SpringOscillatorSimulation.showForce", true);
    const [playing, setPlaying] = useSimState("SpringOscillatorSimulation.playing", false);
    const xRef = useSimRef("SpringOscillatorSimulation.xRef", 0.15);
    const vRef = useSimRef("SpringOscillatorSimulation.vRef", 0);
    const x0Ref = useSimRef("SpringOscillatorSimulation.x0Ref", 0.15);
    const a0Ref = useSimRef("SpringOscillatorSimulation.a0Ref", 0.15);
    const traceRef = useSimRef("SpringOscillatorSimulation.traceRef", []);
    const tRef = useSimRef("SpringOscillatorSimulation.tRef", 0);
    const param = useSimRef("SpringOscillatorSimulation.param", { k, m, mu });
    const raf = useSimRef("SpringOscillatorSimulation.raf", null);
    const lastTs = useSimRef("SpringOscillatorSimulation.lastTs", null);
    const playRef = useSimRef("SpringOscillatorSimulation.playRef", false);
    const dragRef = useSimRef("SpringOscillatorSimulation.dragRef", false);
    const firstRef = useSimRef("SpringOscillatorSimulation.firstRef", false);
    (0, import_react13.useEffect)(() => {
      param.current = { k, m, mu };
    }, [k, m, mu]);
    (0, import_react13.useEffect)(() => {
      playRef.current = playing;
    }, [playing]);
    const fireFirst = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const draw = (0, import_react13.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W3, H3);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W3, H3);
      ctx.fillStyle = "#f8fafc";
      ctx.fillRect(0, 0, W3, H3);
      const x = xRef.current;
      const bx = EQX + Math.max(-0.46, Math.min(0.46, x)) * SCALE2;
      const half = 22;
      ctx.fillStyle = "#e2e8f0";
      ctx.fillRect(0, CY + half, W3, 6);
      ctx.fillStyle = "#94a3b8";
      ctx.fillRect(WALLX - 10, CY - half - 12, 10, half * 2 + 24);
      ctx.strokeStyle = "#cbd5e1";
      ctx.setLineDash([4, 4]);
      ctx.beginPath();
      ctx.moveTo(EQX, CY - half - 16);
      ctx.lineTo(EQX, CY + half + 6);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#94a3b8";
      ctx.font = "11px sans-serif";
      ctx.fillText("VTCB", EQX - 14, CY - half - 20);
      const x1 = WALLX, x2 = bx - half, coils = 14;
      ctx.strokeStyle = "#64748b";
      ctx.lineWidth = 2.5;
      ctx.beginPath();
      ctx.moveTo(x1, CY);
      for (let i = 1; i < coils; i++) {
        const t = i / coils;
        const xx = x1 + (x2 - x1) * t;
        const yy = CY + (i % 2 ? -12 : 12);
        ctx.lineTo(xx, yy);
      }
      ctx.lineTo(x2, CY);
      ctx.stroke();
      const g = ctx.createLinearGradient(0, CY - half, 0, CY + half);
      g.addColorStop(0, "#7dd3fc");
      g.addColorStop(1, "#0284c7");
      ctx.fillStyle = g;
      ctx.fillRect(bx - half, CY - half, half * 2, half * 2);
      ctx.strokeStyle = "#075985";
      ctx.lineWidth = 2;
      ctx.strokeRect(bx - half, CY - half, half * 2, half * 2);
      ctx.fillStyle = "#fff";
      ctx.font = "bold 11px sans-serif";
      ctx.textAlign = "center";
      ctx.fillText(`${m}kg`, bx, CY + 4);
      ctx.textAlign = "left";
      if (showForce && Math.abs(x) > 2e-3) {
        const fpx = Math.max(-150, Math.min(150, -k * x * 16)), y0 = CY, d = Math.sign(fpx);
        ctx.strokeStyle = "#16a34a";
        ctx.fillStyle = "#16a34a";
        ctx.lineWidth = 3;
        ctx.beginPath();
        ctx.moveTo(bx, y0);
        ctx.lineTo(bx + fpx, y0);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(bx + fpx, y0);
        ctx.lineTo(bx + fpx - 9 * d, y0 - 5);
        ctx.lineTo(bx + fpx - 9 * d, y0 + 5);
        ctx.closePath();
        ctx.fill();
        ctx.font = "bold 12px sans-serif";
        ctx.fillText("F k\xE9o v\u1EC1", bx + fpx / 2 - 24, y0 - 9);
      }
      const gx0 = 56, gw = W3 - gx0 - 30, gAxisY = 206, gAmp = 70;
      const yTop = gAxisY - gAmp - 16, xEnd = gx0 + gw;
      ctx.strokeStyle = "#475569";
      ctx.fillStyle = "#475569";
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(gx0, gAxisY);
      ctx.lineTo(xEnd, gAxisY);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(xEnd, gAxisY);
      ctx.lineTo(xEnd - 9, gAxisY - 5);
      ctx.lineTo(xEnd - 9, gAxisY + 5);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(gx0, gAxisY + gAmp + 8);
      ctx.lineTo(gx0, yTop);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(gx0, yTop);
      ctx.lineTo(gx0 - 5, yTop + 9);
      ctx.lineTo(gx0 + 5, yTop + 9);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("x(cm)", gx0 - 4, yTop - 4);
      ctx.fillText("t(s)", xEnd - 6, gAxisY + 18);
      const A0 = a0Ref.current;
      const ampScale = Math.max(XMAX, A0 || XMAX);
      const tr = traceRef.current;
      const tEnd = tRef.current, tStart = Math.max(0, tEnd - WIN);
      const gMap = (p) => ({
        gx: gx0 + (p.t - tStart) / WIN * gw,
        gy: gAxisY - Math.max(-1.1, Math.min(1.1, p.x / ampScale)) * gAmp
      });
      if (mu > 0 && tr.length > 2) {
        const dead = mu * G2 / (k / m);
        const ups = [{ t: 0, v: A0 }];
        const downs = [{ t: 0, v: -A0 }];
        for (let i = 1; i < tr.length - 1; i++) {
          const p = tr[i].x, a = tr[i - 1].x, c = tr[i + 1].x;
          if (p > a && p >= c)
            ups.push({ t: tr[i].t, v: p });
          else if (p < a && p <= c)
            downs.push({ t: tr[i].t, v: p });
        }
        const extend = (pts, sgn) => {
          if (pts.length < 2)
            return;
          const b = pts[pts.length - 1], a = pts[pts.length - 2];
          if (b.t < tEnd - 1e-3) {
            const slope = (b.v - a.v) / Math.max(1e-3, b.t - a.t);
            let ve = b.v + slope * (tEnd - b.t);
            ve = sgn > 0 ? Math.max(dead, ve) : Math.min(-dead, ve);
            pts.push({ t: tEnd, v: ve });
          }
        };
        extend(ups, 1);
        extend(downs, -1);
        const env = (pts) => {
          const w = pts.filter((p) => p.t >= tStart - 1e-3);
          if (w.length < 2)
            return;
          ctx.strokeStyle = "rgba(244,63,94,0.75)";
          ctx.lineWidth = 1.5;
          ctx.setLineDash([6, 4]);
          ctx.beginPath();
          w.forEach((p, k2) => {
            const gx = gx0 + (p.t - tStart) / WIN * gw;
            const gy = gAxisY - Math.max(-1.1, Math.min(1.1, p.v / ampScale)) * gAmp;
            k2 ? ctx.lineTo(gx, gy) : ctx.moveTo(gx, gy);
          });
          ctx.stroke();
          ctx.setLineDash([]);
        };
        env(ups);
        env(downs);
      } else {
        const ay = A0 / ampScale * gAmp;
        ctx.strokeStyle = "rgba(244,63,94,0.35)";
        ctx.setLineDash([5, 4]);
        ctx.beginPath();
        ctx.moveTo(gx0, gAxisY - ay);
        ctx.lineTo(xEnd, gAxisY - ay);
        ctx.moveTo(gx0, gAxisY + ay);
        ctx.lineTo(xEnd, gAxisY + ay);
        ctx.stroke();
        ctx.setLineDash([]);
      }
      if (tr.length > 1) {
        ctx.strokeStyle = "#2563eb";
        ctx.lineWidth = 2;
        ctx.beginPath();
        let started = false;
        for (const p of tr) {
          if (p.t < tStart)
            continue;
          const g2 = gMap(p);
          started ? ctx.lineTo(g2.gx, g2.gy) : (ctx.moveTo(g2.gx, g2.gy), started = true);
        }
        ctx.stroke();
      }
      const omega = Math.sqrt(k / m), T2 = 2 * Math.PI / omega;
      ctx.fillStyle = "#0f172a";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText(`\u03C9 = \u221A(k/m) = ${omega.toFixed(2)} rad/s \xB7 T = ${T2.toFixed(2)} s`, gx0, 330);
      ctx.fillStyle = "#dc2626";
      ctx.fillText(`Bi\xEAn \u0111\u1ED9 A\u2080 = ${(A0 * 100).toFixed(1)} cm  (x\u2080 = ${(x0Ref.current * 100).toFixed(1)} cm, v\u2080 = ${v0} m/s)`, gx0, 354);
      if (mu > 0) {
        ctx.fillStyle = "#7c3aed";
        ctx.fillText("ma s\xE1t \u2192 t\u1EAFt d\u1EA7n (bi\xEAn \u0111\u1ED9 gi\u1EA3m tuy\u1EBFn t\xEDnh)", xEnd - 290, 330);
      }
    }, [k, m, mu, v0, showForce]);
    const loop = (0, import_react13.useCallback)((ts) => {
      if (!playRef.current)
        return;
      if (lastTs.current == null)
        lastTs.current = ts;
      let dt = (ts - lastTs.current) / 1e3;
      lastTs.current = ts;
      dt = Math.min(dt, 0.03);
      const { k: kk, m: mm, mu: mmu } = param.current;
      let x = xRef.current, v = vRef.current;
      const w2 = kk / mm, fr = mmu * G2;
      let a = 0, settle = false;
      if (Math.abs(v) < 0.02 && w2 * Math.abs(x) <= fr + 1e-6)
        settle = true;
      else if (Math.abs(v) < 0.02)
        a = -w2 * x + fr * Math.sign(x);
      else
        a = -w2 * x - fr * Math.sign(v);
      let done = false;
      if (settle) {
        x *= 0.82;
        v = 0;
        if (Math.abs(x) < 4e-3) {
          x = 0;
          done = true;
        }
      } else {
        v += a * dt;
        x += v * dt;
      }
      xRef.current = x;
      vRef.current = v;
      tRef.current += dt;
      traceRef.current.push({ t: tRef.current, x });
      if (traceRef.current.length > 2e3)
        traceRef.current.shift();
      draw();
      if (done) {
        setPlaying(false);
        return;
      }
      if (playRef.current)
        raf.current = requestAnimationFrame(loop);
    }, [draw]);
    (0, import_react13.useEffect)(() => {
      if (playing) {
        lastTs.current = null;
        raf.current = requestAnimationFrame(loop);
      }
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [playing, loop]);
    (0, import_react13.useEffect)(() => {
      if (!playing)
        draw();
    }, [draw, playing]);
    const launch = () => {
      fireFirst();
      xRef.current = x0Ref.current;
      vRef.current = v0;
      tRef.current = 0;
      traceRef.current = [];
      const omega = Math.sqrt(k / m);
      a0Ref.current = Math.sqrt(x0Ref.current * x0Ref.current + v0 / omega * (v0 / omega));
      setPlaying(true);
    };
    const toLogic = (e) => {
      const r = cv.current.getBoundingClientRect();
      return (e.clientX - r.left) / r.width * W3;
    };
    const onDown = (e) => {
      const px2 = toLogic(e);
      const bx = EQX + xRef.current * SCALE2;
      if (Math.abs(px2 - bx) < 40) {
        dragRef.current = true;
        setPlaying(false);
        fireFirst();
        e.target.setPointerCapture?.(e.pointerId);
      }
    };
    const onMove = (e) => {
      if (!dragRef.current)
        return;
      const px2 = toLogic(e);
      let x = (px2 - EQX) / SCALE2;
      x = Math.max(-XMAX, Math.min(XMAX, x));
      xRef.current = x;
      x0Ref.current = x;
      a0Ref.current = Math.abs(x);
      vRef.current = 0;
      draw();
    };
    const onUp = () => {
      if (dragRef.current) {
        dragRef.current = false;
        launch();
      }
    };
    const reset = () => {
      setPlaying(false);
      xRef.current = 0;
      x0Ref.current = 0;
      a0Ref.current = 0;
      vRef.current = 0;
      traceRef.current = [];
      tRef.current = 0;
      draw();
    };
    const sf = (s) => (v) => {
      fireFirst();
      s(v);
    };
    return /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("canvas", { ref: cv, width: W3, height: H3, onPointerDown: onDown, onPointerMove: onMove, onPointerUp: onUp, onPointerLeave: onUp, className: "w-full rounded-lg border border-gray-100 bg-slate-50 touch-none cursor-grab active:cursor-grabbing" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("div", { className: "flex items-center justify-center gap-2 my-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("button", { onClick: launch, className: "px-4 py-2 rounded-xl bg-sky-600 text-white text-sm font-bold hover:bg-sky-700", "data-nb-play": "", children: "\u25B6 Th\u1EA3 / Ch\u1EA1y l\u1EA1i" }),
          /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("button", { onClick: () => setPlaying(false), className: "px-4 py-2 rounded-xl bg-amber-500 text-white text-sm font-bold hover:bg-amber-600", children: "\u23F8 D\u1EEBng" }),
          /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("button", { onClick: reset, className: "px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-sm font-bold hover:bg-slate-200", children: "\u21BA V\u1EC1 VTCB" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("label", { className: "flex items-center gap-2 text-sm mb-2 cursor-pointer", children: [
          /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("input", { type: "checkbox", checked: showForce, onChange: (e) => {
            fireFirst();
            setShowForce(e.target.checked);
          } }),
          " ",
          /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("span", { className: "text-emerald-700 font-semibold", children: "Hi\u1EC7n l\u1EF1c k\xE9o v\u1EC1 (l\u1EF1c \u0111\xE0n h\u1ED3i)" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("div", { className: "grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-2 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("span", { className: "w-28 text-gray-600", children: "\u0110\u1ED9 c\u1EE9ng k" }),
            /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("input", { type: "range", min: 5, max: 50, step: 1, value: k, onChange: (e) => sf(setK)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("span", { className: "w-16 text-right font-semibold", children: [
              k,
              " N/m"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("span", { className: "w-28 text-gray-600", children: "Kh\u1ED1i l\u01B0\u1EE3ng m" }),
            /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("input", { type: "range", min: 0.5, max: 3, step: 0.5, value: m, onChange: (e) => sf(setM)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("span", { className: "w-16 text-right font-semibold", children: [
              m,
              " kg"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("span", { className: "w-28 text-gray-600", children: "H\u1EC7 s\u1ED1 ma s\xE1t \u03BC" }),
            /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("input", { type: "range", min: 0, max: 0.3, step: 0.01, value: mu, onChange: (e) => sf(setMu)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("span", { className: "w-16 text-right font-semibold", children: mu.toFixed(2) })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("span", { className: "w-28 text-gray-600", children: "V\u1EADn t\u1ED1c \u0111\u1EA7u v\u2080" }),
            /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("input", { type: "range", min: 0, max: 3, step: 0.5, value: v0, onChange: (e) => sf(setV0)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("span", { className: "w-16 text-right font-semibold", children: [
              v0,
              " m/s"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime10.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("h4", { className: "font-bold text-sky-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "K\xE9o v\u1EADt ra kh\u1ECFi VTCB r\u1ED3i th\u1EA3 tay \u0111\u1EC3 dao \u0111\u1ED9ng. v\u2080=0 \u2192 bi\xEAn \u0111\u1ED9 \u0111\xFAng b\u1EB1ng x\u2080; v\u2080>0 \u2192 bi\xEAn \u0111\u1ED9 l\u1EDBn h\u01A1n. T\u0103ng h\u1EC7 s\u1ED1 c\u1EA3n \u0111\u1EC3 th\u1EA5y bi\xEAn \u0111\u1ED9 gi\u1EA3m d\u1EA7n (t\u1EAFt d\u1EA7n)." })
        ] })
      ] })
    ] });
  };
  var SpringOscillatorSimulation_default = SpringOscillatorSimulation;

  // physics/vendor/simulations/SimplePendulumSimulation.tsx
  var import_react14 = __toESM(require_react());
  var import_jsx_runtime11 = __toESM(require_jsx_runtime());
  var G3 = 9.8;
  var W4 = 660;
  var H4 = 360;
  var PVX = 175;
  var PVY = 36;
  var WIN2 = 8;
  var THMAX = 1.35;
  var SimplePendulumSimulation = ({ title = "Con l\u1EAFc \u0111\u01A1n \u2014 k\xE9o & th\u1EA3 (t\u1EAFt d\u1EA7n)", description = "K\xE9o v\u1EADt t\u1EDBi g\xF3c \u03B8\u2080 r\u1ED3i th\u1EA3: bi\xEAn \u0111\u1ED9 g\xF3c b\u1EB1ng \u03B8\u2080. C\u1EA5p v\u1EADn t\u1ED1c \u0111\u1EA7u v\u2080 \u2192 bi\xEAn \u0111\u1ED9 l\u1EDBn h\u01A1n. T\u0103ng h\u1EC7 s\u1ED1 c\u1EA3n \u2192 dao \u0111\u1ED9ng t\u1EAFt d\u1EA7n. Chu k\xEC T=2\u03C0\u221A(l/g) kh\xF4ng ph\u1EE5 thu\u1ED9c kh\u1ED1i l\u01B0\u1EE3ng.", onComplete }) => {
    const cv = useSimRef("SimplePendulumSimulation.cv", null);
    const [l, setL] = useSimState("SimplePendulumSimulation.l", 1.2);
    const [c, setC] = useSimState("SimplePendulumSimulation.c", 0);
    const [v0, setV0] = useSimState("SimplePendulumSimulation.v0", 0);
    const [showForce, setShowForce] = useSimState("SimplePendulumSimulation.showForce", true);
    const [playing, setPlaying] = useSimState("SimplePendulumSimulation.playing", false);
    const thRef = useSimRef("SimplePendulumSimulation.thRef", 0.5);
    const omRef = useSimRef("SimplePendulumSimulation.omRef", 0);
    const th0Ref = useSimRef("SimplePendulumSimulation.th0Ref", 0.5);
    const a0Ref = useSimRef("SimplePendulumSimulation.a0Ref", 0.5);
    const traceRef = useSimRef("SimplePendulumSimulation.traceRef", []);
    const tRef = useSimRef("SimplePendulumSimulation.tRef", 0);
    const param = useSimRef("SimplePendulumSimulation.param", { l, c });
    const raf = useSimRef("SimplePendulumSimulation.raf", null);
    const lastTs = useSimRef("SimplePendulumSimulation.lastTs", null);
    const playRef = useSimRef("SimplePendulumSimulation.playRef", false);
    const dragRef = useSimRef("SimplePendulumSimulation.dragRef", false);
    const firstRef = useSimRef("SimplePendulumSimulation.firstRef", false);
    (0, import_react14.useEffect)(() => {
      param.current = { l, c };
    }, [l, c]);
    (0, import_react14.useEffect)(() => {
      playRef.current = playing;
    }, [playing]);
    const fireFirst = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const Lpix = () => 50 + l * 80;
    const draw = (0, import_react14.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W4, H4);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W4, H4);
      ctx.fillStyle = "#f8fafc";
      ctx.fillRect(0, 0, W4, H4);
      const Lp = Lpix(), th = thRef.current;
      const bx = PVX + Lp * Math.sin(th), by = PVY + Lp * Math.cos(th);
      ctx.fillStyle = "#94a3b8";
      ctx.fillRect(PVX - 42, PVY - 9, 84, 9);
      ctx.strokeStyle = "#cbd5e1";
      ctx.setLineDash([4, 4]);
      ctx.beginPath();
      ctx.moveTo(PVX, PVY);
      ctx.lineTo(PVX, PVY + Lp + 26);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.strokeStyle = "#7c3aed";
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.arc(PVX, PVY, 34, Math.PI / 2, Math.PI / 2 - th, th > 0);
      ctx.stroke();
      ctx.fillStyle = "#6d28d9";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText(`\u03B8=${(th * 180 / Math.PI).toFixed(0)}\xB0`, PVX + 12, PVY + 50);
      ctx.strokeStyle = "#475569";
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(PVX, PVY);
      ctx.lineTo(bx, by);
      ctx.stroke();
      ctx.fillStyle = "#475569";
      ctx.beginPath();
      ctx.arc(PVX, PVY, 3.5, 0, Math.PI * 2);
      ctx.fill();
      const g = ctx.createRadialGradient(bx - 5, by - 5, 3, bx, by, 20);
      g.addColorStop(0, "#fda4af");
      g.addColorStop(1, "#e11d48");
      ctx.fillStyle = g;
      ctx.beginPath();
      ctx.arc(bx, by, 18, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = "#9f1239";
      ctx.lineWidth = 2;
      ctx.stroke();
      if (showForce && Math.abs(th) > 0.02) {
        const fmag = Math.min(72, Math.abs(Math.sin(th)) * 95);
        const s2 = -Math.sign(th);
        const fx = s2 * Math.cos(th) * fmag, fy = s2 * -Math.sin(th) * fmag;
        const L = Math.hypot(fx, fy) || 1, ux = fx / L, uy = fy / L;
        ctx.strokeStyle = "#16a34a";
        ctx.fillStyle = "#16a34a";
        ctx.lineWidth = 3;
        ctx.beginPath();
        ctx.moveTo(bx, by);
        ctx.lineTo(bx + fx, by + fy);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(bx + fx, by + fy);
        ctx.lineTo(bx + fx - 9 * ux - 5 * uy, by + fy - 9 * uy + 5 * ux);
        ctx.lineTo(bx + fx - 9 * ux + 5 * uy, by + fy - 9 * uy - 5 * ux);
        ctx.closePath();
        ctx.fill();
        ctx.font = "bold 12px sans-serif";
        ctx.fillText("F k\xE9o v\u1EC1", bx + fx + ux * 6 - 8, by + fy + uy * 6 + 4);
      }
      const gx0 = 360, gw = W4 - gx0 - 24, gAxisY = 150, gAmp = 84;
      const yTop = gAxisY - gAmp - 16, xEnd = gx0 + gw;
      ctx.strokeStyle = "#475569";
      ctx.fillStyle = "#475569";
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(gx0, gAxisY);
      ctx.lineTo(xEnd, gAxisY);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(xEnd, gAxisY);
      ctx.lineTo(xEnd - 9, gAxisY - 5);
      ctx.lineTo(xEnd - 9, gAxisY + 5);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(gx0, gAxisY + gAmp + 8);
      ctx.lineTo(gx0, yTop);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(gx0, yTop);
      ctx.lineTo(gx0 - 5, yTop + 9);
      ctx.lineTo(gx0 + 5, yTop + 9);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("\u03B8(\xB0)", gx0 - 4, yTop - 4);
      ctx.fillText("t(s)", xEnd - 6, gAxisY + 18);
      const A0 = a0Ref.current;
      const tr = traceRef.current;
      const tEnd = tRef.current, tStart = Math.max(0, tEnd - WIN2);
      const pMap = (p) => ({
        gx: gx0 + (p.t - tStart) / WIN2 * gw,
        gy: gAxisY - Math.max(-1.1, Math.min(1.1, p.th / THMAX)) * gAmp
      });
      if (c > 0 && tr.length > 2) {
        const ups = [{ t: 0, v: A0 }];
        const downs = [{ t: 0, v: -A0 }];
        for (let i = 1; i < tr.length - 1; i++) {
          const p = tr[i].th, a = tr[i - 1].th, c2 = tr[i + 1].th;
          if (p > a && p >= c2)
            ups.push({ t: tr[i].t, v: p });
          else if (p < a && p <= c2)
            downs.push({ t: tr[i].t, v: p });
        }
        const extend = (pts, sgn) => {
          if (pts.length < 2)
            return;
          const b = pts[pts.length - 1], a = pts[pts.length - 2];
          if (b.t < tEnd - 1e-3) {
            const slope = (b.v - a.v) / Math.max(1e-3, b.t - a.t);
            let ve = b.v + slope * (tEnd - b.t);
            ve = sgn > 0 ? Math.max(0, ve) : Math.min(0, ve);
            pts.push({ t: tEnd, v: ve });
          }
        };
        extend(ups, 1);
        extend(downs, -1);
        const env = (pts) => {
          const w = pts.filter((p) => p.t >= tStart - 1e-3);
          if (w.length < 2)
            return;
          ctx.strokeStyle = "rgba(244,63,94,0.75)";
          ctx.lineWidth = 1.5;
          ctx.setLineDash([6, 4]);
          ctx.beginPath();
          w.forEach((p, k) => {
            const gx = gx0 + (p.t - tStart) / WIN2 * gw;
            const gy = gAxisY - Math.max(-1.1, Math.min(1.1, p.v / THMAX)) * gAmp;
            k ? ctx.lineTo(gx, gy) : ctx.moveTo(gx, gy);
          });
          ctx.stroke();
          ctx.setLineDash([]);
        };
        env(ups);
        env(downs);
      } else {
        const ay = A0 / THMAX * gAmp;
        ctx.strokeStyle = "rgba(244,63,94,0.35)";
        ctx.setLineDash([5, 4]);
        ctx.beginPath();
        ctx.moveTo(gx0, gAxisY - ay);
        ctx.lineTo(gx0 + gw, gAxisY - ay);
        ctx.moveTo(gx0, gAxisY + ay);
        ctx.lineTo(gx0 + gw, gAxisY + ay);
        ctx.stroke();
        ctx.setLineDash([]);
      }
      if (tr.length > 1) {
        ctx.strokeStyle = "#e11d48";
        ctx.lineWidth = 2;
        ctx.beginPath();
        let started = false;
        for (const p of tr) {
          if (p.t < tStart)
            continue;
          const g2 = pMap(p);
          started ? ctx.lineTo(g2.gx, g2.gy) : (ctx.moveTo(g2.gx, g2.gy), started = true);
        }
        ctx.stroke();
      }
      const T2 = 2 * Math.PI * Math.sqrt(l / G3);
      ctx.fillStyle = "#0f172a";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText(`T = 2\u03C0\u221A(l/g) = ${T2.toFixed(2)} s`, gx0, 300);
      ctx.fillStyle = "#dc2626";
      ctx.fillText(`Bi\xEAn \u0111\u1ED9 \u03B8\u2080 = ${(th0Ref.current * 180 / Math.PI).toFixed(0)}\xB0 \xB7 A\u2080 = ${(A0 * 180 / Math.PI).toFixed(0)}\xB0 (v\u2080 = ${v0} m/s)`, gx0, 322);
      if (c > 0) {
        ctx.fillStyle = "#7c3aed";
        ctx.fillText("c\u1EA3n nh\u1EDBt \u2192 t\u1EAFt d\u1EA7n (bi\xEAn \u0111\u1ED9 gi\u1EA3m h\xE0m m\u0169)", gx0, 342);
      }
    }, [l, c, v0, showForce]);
    const loop = (0, import_react14.useCallback)((ts) => {
      if (!playRef.current)
        return;
      if (lastTs.current == null)
        lastTs.current = ts;
      let dt = (ts - lastTs.current) / 1e3;
      lastTs.current = ts;
      dt = Math.min(dt, 0.03);
      const { l: ll, c: cc } = param.current;
      let th = thRef.current, om = omRef.current;
      const alpha = -(G3 / ll) * Math.sin(th) - cc * om;
      om += alpha * dt;
      th += om * dt;
      if (th > THMAX) {
        th = THMAX;
        om = -om * 0.4;
      } else if (th < -THMAX) {
        th = -THMAX;
        om = -om * 0.4;
      }
      thRef.current = th;
      omRef.current = om;
      tRef.current += dt;
      traceRef.current.push({ t: tRef.current, th });
      if (traceRef.current.length > 2e3)
        traceRef.current.shift();
      draw();
      if (playRef.current)
        raf.current = requestAnimationFrame(loop);
    }, [draw]);
    (0, import_react14.useEffect)(() => {
      if (playing) {
        lastTs.current = null;
        raf.current = requestAnimationFrame(loop);
      }
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [playing, loop]);
    (0, import_react14.useEffect)(() => {
      if (!playing)
        draw();
    }, [draw, playing]);
    const launch = () => {
      fireFirst();
      const om0 = v0 / l;
      thRef.current = th0Ref.current;
      omRef.current = om0;
      tRef.current = 0;
      traceRef.current = [];
      const wn = Math.sqrt(G3 / l);
      a0Ref.current = Math.min(THMAX, Math.sqrt(th0Ref.current * th0Ref.current + om0 / wn * (om0 / wn)));
      setPlaying(true);
    };
    const toLogic = (e) => {
      const r = cv.current.getBoundingClientRect();
      return { x: (e.clientX - r.left) / r.width * W4, y: (e.clientY - r.top) / r.height * H4 };
    };
    const onDown = (e) => {
      const p = toLogic(e);
      const Lp = Lpix();
      const th = thRef.current;
      const bx = PVX + Lp * Math.sin(th), by = PVY + Lp * Math.cos(th);
      if (Math.hypot(p.x - bx, p.y - by) < 32) {
        dragRef.current = true;
        setPlaying(false);
        fireFirst();
        e.target.setPointerCapture?.(e.pointerId);
      }
    };
    const onMove = (e) => {
      if (!dragRef.current)
        return;
      const p = toLogic(e);
      let th = Math.atan2(p.x - PVX, p.y - PVY);
      th = Math.max(-THMAX, Math.min(THMAX, th));
      thRef.current = th;
      th0Ref.current = th;
      a0Ref.current = Math.abs(th);
      omRef.current = 0;
      draw();
    };
    const onUp = () => {
      if (dragRef.current) {
        dragRef.current = false;
        launch();
      }
    };
    const reset = () => {
      setPlaying(false);
      thRef.current = 0;
      th0Ref.current = 0;
      a0Ref.current = 0;
      omRef.current = 0;
      traceRef.current = [];
      tRef.current = 0;
      draw();
    };
    const sf = (s) => (v) => {
      fireFirst();
      s(v);
    };
    return /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("canvas", { ref: cv, width: W4, height: H4, onPointerDown: onDown, onPointerMove: onMove, onPointerUp: onUp, onPointerLeave: onUp, className: "w-full rounded-lg border border-gray-100 bg-slate-50 touch-none cursor-grab active:cursor-grabbing" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("div", { className: "flex items-center justify-center gap-2 my-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("button", { onClick: launch, className: "px-4 py-2 rounded-xl bg-rose-600 text-white text-sm font-bold hover:bg-rose-700", "data-nb-play": "", children: "\u25B6 Th\u1EA3 / Ch\u1EA1y l\u1EA1i" }),
          /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("button", { onClick: () => setPlaying(false), className: "px-4 py-2 rounded-xl bg-amber-500 text-white text-sm font-bold hover:bg-amber-600", children: "\u23F8 D\u1EEBng" }),
          /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("button", { onClick: reset, className: "px-4 py-2 rounded-xl bg-slate-100 text-slate-700 text-sm font-bold hover:bg-slate-200", children: "\u21BA V\u1EC1 VTCB" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("label", { className: "flex items-center gap-2 text-sm mb-2 cursor-pointer", children: [
          /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("input", { type: "checkbox", checked: showForce, onChange: (e) => {
            fireFirst();
            setShowForce(e.target.checked);
          } }),
          " ",
          /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("span", { className: "text-emerald-700 font-semibold", children: "Hi\u1EC7n l\u1EF1c k\xE9o v\u1EC1 (th\xE0nh ph\u1EA7n ti\u1EBFp tuy\u1EBFn c\u1EE7a tr\u1ECDng l\u1EF1c)" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("div", { className: "grid grid-cols-1 sm:grid-cols-3 gap-x-4 gap-y-2 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("span", { className: "w-24 text-gray-600", children: "Chi\u1EC1u d\xE0i l" }),
            /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("input", { type: "range", min: 0.5, max: 2.5, step: 0.1, value: l, onChange: (e) => sf(setL)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("span", { className: "w-14 text-right font-semibold", children: [
              l.toFixed(1),
              " m"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("span", { className: "w-24 text-gray-600", children: "H\u1EC7 s\u1ED1 c\u1EA3n" }),
            /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("input", { type: "range", min: 0, max: 2, step: 0.05, value: c, onChange: (e) => sf(setC)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("span", { className: "w-14 text-right font-semibold", children: c.toFixed(2) })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("span", { className: "w-24 text-gray-600", children: "V\u1EADn t\u1ED1c \u0111\u1EA7u v\u2080" }),
            /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("input", { type: "range", min: 0, max: 3, step: 0.5, value: v0, onChange: (e) => sf(setV0)(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("span", { className: "w-14 text-right font-semibold", children: [
              v0,
              " m/s"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("h4", { className: "font-bold text-rose-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "K\xE9o qu\u1EA3 n\u1EB7ng l\u1EC7ch kh\u1ECFi ph\u01B0\u01A1ng th\u1EB3ng \u0111\u1EE9ng r\u1ED3i th\u1EA3. v\u2080=0 \u2192 bi\xEAn \u0111\u1ED9 \u0111\xFAng b\u1EB1ng \u03B8\u2080; v\u2080>0 \u2192 bi\xEAn \u0111\u1ED9 l\u1EDBn h\u01A1n. \u0110\u1ED5i l \u0111\u1EC3 th\u1EA5y chu k\xEC \u0111\u1ED5i; \u0111\u1ED5i h\u1EC7 s\u1ED1 c\u1EA3n \u0111\u1EC3 th\u1EA5y t\u1EAFt d\u1EA7n." })
        ] })
      ] })
    ] });
  };
  var SimplePendulumSimulation_default = SimplePendulumSimulation;

  // physics/vendor/simulations/EnergyOscillationSimulation.tsx
  var import_react15 = __toESM(require_react());
  var import_jsx_runtime12 = __toESM(require_jsx_runtime());
  var EnergyOscillationSimulation = ({ title = "N\u0103ng l\u01B0\u1EE3ng trong dao \u0111\u1ED9ng \u0111i\u1EC1u ho\xE0", description = "C\u01A1 n\u0103ng W = W\u0111 + Wt kh\xF4ng \u0111\u1ED5i. T\u1EA1i bi\xEAn: to\xE0n th\u1EBF n\u0103ng; t\u1EA1i VTCB: to\xE0n \u0111\u1ED9ng n\u0103ng.", onComplete }) => {
    const cv = useSimRef("EnergyOscillationSimulation.cv", null);
    const raf = useSimRef("EnergyOscillationSimulation.raf", null);
    const lastRef = useSimRef("EnergyOscillationSimulation.lastRef", null);
    const tRef = useSimRef("EnergyOscillationSimulation.tRef", 0);
    const [running2, setRunning] = useSimState("EnergyOscillationSimulation.running", true);
    const [A, setA] = useSimState("EnergyOscillationSimulation.A", 1);
    const [omega, setOmega] = useSimState("EnergyOscillationSimulation.omega", 2);
    const phaseRef = useSimRef("EnergyOscillationSimulation.phaseRef", 0);
    const firstRef = useSimRef("EnergyOscillationSimulation.firstRef", false);
    const W12 = 680, H12 = 280;
    const XC = 200, AMP2 = 90;
    const draw = (0, import_react15.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      const t = tRef.current;
      const ph = omega * t + phaseRef.current;
      const cosx = Math.cos(ph), sinx = Math.sin(ph);
      const Wtot = 1;
      const Wt = Wtot * cosx * cosx;
      const Wd = Wtot * sinx * sinx;
      const cy = 110, half = 22, wallX = 56;
      const mx = XC + A * cosx * AMP2;
      ctx.fillStyle = "#e2e8f0";
      ctx.fillRect(0, cy + half, 400, 6);
      ctx.fillStyle = "#94a3b8";
      ctx.fillRect(wallX - 10, cy - half - 12, 10, half * 2 + 24);
      ctx.strokeStyle = "#cbd5e1";
      ctx.setLineDash([4, 4]);
      ctx.beginPath();
      ctx.moveTo(XC, cy - half - 16);
      ctx.lineTo(XC, cy + half + 6);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#94a3b8";
      ctx.font = "11px sans-serif";
      ctx.fillText("VTCB", XC - 14, cy - half - 20);
      const x1 = wallX, x2 = mx - half, coils = 14;
      ctx.strokeStyle = "#64748b";
      ctx.lineWidth = 2.5;
      ctx.beginPath();
      ctx.moveTo(x1, cy);
      for (let i = 1; i < coils; i++) {
        const t2 = i / coils;
        const xx = x1 + (x2 - x1) * t2;
        const yy = cy + (i % 2 ? -12 : 12);
        ctx.lineTo(xx, yy);
      }
      ctx.lineTo(x2, cy);
      ctx.stroke();
      const g = ctx.createLinearGradient(0, cy - half, 0, cy + half);
      g.addColorStop(0, "#7dd3fc");
      g.addColorStop(1, "#0284c7");
      ctx.fillStyle = g;
      ctx.fillRect(mx - half, cy - half, half * 2, half * 2);
      ctx.strokeStyle = "#075985";
      ctx.lineWidth = 2;
      ctx.strokeRect(mx - half, cy - half, half * 2, half * 2);
      const bx = 430, bw = 60, bh = 180, by = 220, gap = 80;
      const bar = (x, val, color, label) => {
        ctx.fillStyle = "#e2e8f0";
        ctx.fillRect(x, by - bh, bw, bh);
        ctx.fillStyle = color;
        ctx.fillRect(x, by - bh * val, bw, bh * val);
        ctx.strokeStyle = "#94a3b8";
        ctx.strokeRect(x, by - bh, bw, bh);
        ctx.fillStyle = "#334155";
        ctx.font = "bold 12px sans-serif";
        ctx.textAlign = "center";
        ctx.fillText(label, x + bw / 2, by + 16);
        ctx.fillText(`${Math.round(val * 100)}%`, x + bw / 2, by - bh * val - 6);
        ctx.textAlign = "left";
      };
      bar(bx, Wd, "#10b981", "W\u0111");
      bar(bx + gap, Wt, "#3b82f6", "Wt");
      bar(bx + gap * 2, Wd + Wt, "#f59e0b", "W");
    }, [A, omega]);
    const tick = (0, import_react15.useCallback)((ts) => {
      if (lastRef.current == null)
        lastRef.current = ts;
      const dt = Math.min(0.05, (ts - lastRef.current) / 1e3);
      lastRef.current = ts;
      tRef.current += dt;
      draw();
      raf.current = requestAnimationFrame(tick);
    }, [draw]);
    (0, import_react15.useEffect)(() => {
      if (running2) {
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        lastRef.current = null;
        raf.current = requestAnimationFrame(tick);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, tick, onComplete]);
    (0, import_react15.useEffect)(() => {
      draw();
    }, [draw]);
    const [, bump] = (0, import_react15.useState)(0);
    const dragRef = useSimRef("EnergyOscillationSimulation.dragRef", false);
    const toX = (e) => {
      const r = cv.current.getBoundingClientRect();
      return (e.clientX - r.left) / r.width * W12;
    };
    const curMx = () => XC + A * Math.cos(omega * tRef.current + phaseRef.current) * AMP2;
    const onDown = (e) => {
      if (Math.abs(toX(e) - curMx()) < 28) {
        dragRef.current = true;
        setRunning(false);
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        ;
        e.target.setPointerCapture?.(e.pointerId);
      }
    };
    const onMove = (e) => {
      if (!dragRef.current)
        return;
      let xd = (toX(e) - XC) / AMP2;
      xd = Math.max(-1, Math.min(1, xd));
      phaseRef.current = xd >= 0 ? 0 : Math.PI;
      tRef.current = 0;
      setA(Math.max(0.2, Math.abs(xd)));
      bump((n) => n + 1);
    };
    const onUp = () => {
      if (dragRef.current) {
        dragRef.current = false;
        setRunning(true);
      }
    };
    return /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("canvas", { ref: cv, width: W12, height: H12, onPointerDown: onDown, onPointerMove: onMove, onPointerUp: onUp, onPointerLeave: onUp, className: "w-full rounded-lg border border-gray-100 bg-slate-50 touch-none cursor-grab active:cursor-grabbing" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)("div", { className: "space-y-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("span", { className: "w-28 text-gray-600", children: "Bi\xEAn \u0111\u1ED9 A" }),
            /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("input", { type: "range", min: 0.4, max: 1, step: 0.1, value: A, onChange: (e) => setA(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("span", { className: "w-10 text-right font-semibold", children: A.toFixed(1) })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("span", { className: "w-28 text-gray-600", children: "T\u1EA7n s\u1ED1 g\xF3c \u03C9" }),
            /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("input", { type: "range", min: 1, max: 4, step: 0.5, value: omega, onChange: (e) => setOmega(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("span", { className: "w-10 text-right font-semibold", children: omega.toFixed(1) })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("button", { onClick: () => setRunning((r) => !r), className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-emerald-600 text-white font-bold hover:bg-emerald-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)(import_jsx_runtime12.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime12.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)(import_jsx_runtime12.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime12.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)("button", { onClick: () => {
            tRef.current = 0;
            draw();
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime12.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " \u0110\u1EB7t l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("h4", { className: "font-bold text-emerald-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var EnergyOscillationSimulation_default = EnergyOscillationSimulation;

  // physics/components/TwoOscillationsSimulation.tsx
  var import_react16 = __toESM(require_react());
  var import_jsx_runtime13 = __toESM(require_jsx_runtime());
  var W5 = 680;
  var H5 = 300;
  var OMEGA = 1;
  var MIDY = 150;
  var SCALE3 = 78;
  var GX0 = 150;
  var GX1 = 662;
  var WINDOW = 2 * 2 * Math.PI / OMEGA;
  function parsePhase(s) {
    const t = s.trim().toLowerCase().replace(/π/g, "pi");
    if (t === "")
      return 0;
    if (!/^[-+*/().\s0-9pi]*$/.test(t))
      return null;
    try {
      const v = Function('"use strict"; const pi = Math.PI; return (' + t + ")")();
      return Number.isFinite(v) ? v : null;
    } catch {
      return null;
    }
  }
  function fmtPi(r) {
    const k = r / Math.PI;
    const table = [[0, "0"], [1, "\u03C0"], [-1, "\u2212\u03C0"], [0.5, "\u03C0/2"], [-0.5, "\u2212\u03C0/2"], [1 / 3, "\u03C0/3"], [-1 / 3, "\u2212\u03C0/3"], [0.25, "\u03C0/4"], [-0.25, "\u2212\u03C0/4"], [2 / 3, "2\u03C0/3"], [-2 / 3, "\u22122\u03C0/3"], [0.75, "3\u03C0/4"], [-0.75, "\u22123\u03C0/4"], [1 / 6, "\u03C0/6"], [-1 / 6, "\u2212\u03C0/6"]];
    for (const [v, lbl] of table)
      if (Math.abs(k - v) < 0.02)
        return lbl;
    return `${r.toFixed(2)} rad`;
  }
  var TwoOscillationsSimulation = ({ title = "Hai dao \u0111\u1ED9ng \u0111i\u1EC1u ho\xE0 \u2014 \u0111\u1ED9 l\u1EC7ch pha", description = "Hai dao \u0111\u1ED9ng c\xF9ng t\u1EA7n s\u1ED1 x = A\xB7cos(\u03C9t + \u03C6). Ch\u1EC9nh bi\xEAn \u0111\u1ED9 v\xE0 pha ban \u0111\u1EA7u \u0111\u1EC3 th\u1EA5y \u0111\u1ED9 l\u1EC7ch pha \u0394\u03C6 = \u03C6\u2082 \u2212 \u03C6\u2081. Nh\u1EADp pha theo radian (vd: pi/2).", onComplete }) => {
    const cv = useSimRef("TwoOscillationsSimulation.cv", null);
    const raf = useSimRef("TwoOscillationsSimulation.raf", null);
    const tRef = useSimRef("TwoOscillationsSimulation.tRef", 0);
    const lastTs = useSimRef("TwoOscillationsSimulation.lastTs", null);
    const [a1, setA1] = useSimState("TwoOscillationsSimulation.a1", 1), [a2, setA2] = useSimState("TwoOscillationsSimulation.a2", 0.8);
    const [p1s, setP1s] = useSimState("TwoOscillationsSimulation.p1s", "0"), [p2s, setP2s] = useSimState("TwoOscillationsSimulation.p2s", "pi/2");
    const [running2, setRunning] = useSimState("TwoOscillationsSimulation.running", true);
    const phi1 = parsePhase(p1s), phi2 = parsePhase(p2s);
    const refs2 = useSimRef("TwoOscillationsSimulation.refs", { a1, a2, phi1, phi2, running: running2 });
    refs2.current = { a1, a2, phi1: phi1 ?? 0, phi2: phi2 ?? 0, running: running2 };
    const firstRef = useSimRef("TwoOscillationsSimulation.firstRef", false);
    const touch = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const x1 = (t) => refs2.current.a1 * Math.cos(OMEGA * t + refs2.current.phi1);
    const x2 = (t) => refs2.current.a2 * Math.cos(OMEGA * t + refs2.current.phi2);
    const draw = (0, import_react16.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W5, H5);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W5, H5);
      const t = tRef.current, tc = t % WINDOW;
      ctx.strokeStyle = "#e2e8f0";
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(0, MIDY);
      ctx.lineTo(W5, MIDY);
      ctx.stroke();
      const yTop = MIDY - SCALE3 - 30;
      ctx.fillStyle = "#94a3b8";
      ctx.fillRect(26, yTop - 8, 98, 6);
      ctx.strokeStyle = "#cbd5e1";
      ctx.lineWidth = 1;
      for (let hx = 30; hx < 122; hx += 8) {
        ctx.beginPath();
        ctx.moveTo(hx, yTop - 8);
        ctx.lineTo(hx - 5, yTop - 2);
        ctx.stroke();
      }
      const spring = (x, y2) => {
        const n = 9, w = 7, y1 = yTop - 2, len = y2 - 9 - (y1 + 6);
        ctx.strokeStyle = "#64748b";
        ctx.lineWidth = 1.8;
        ctx.beginPath();
        ctx.moveTo(x, y1);
        ctx.lineTo(x, y1 + 6);
        for (let i = 0; i < n; i++)
          ctx.lineTo(x + (i % 2 ? w : -w), y1 + 6 + len * (i + 0.5) / n);
        ctx.lineTo(x, y2 - 9);
        ctx.stroke();
      };
      const drawOsc = (ox, val, color, lbl) => {
        ctx.strokeStyle = "#cbd5e1";
        ctx.setLineDash([3, 3]);
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(ox, MIDY - SCALE3);
        ctx.lineTo(ox, MIDY + SCALE3 + 12);
        ctx.stroke();
        ctx.setLineDash([]);
        const y = MIDY - val * SCALE3;
        spring(ox, y);
        ctx.fillStyle = color;
        ctx.fillRect(ox - 11, y - 9, 22, 22);
        ctx.fillStyle = "#fff";
        ctx.font = "bold 12px sans-serif";
        ctx.textAlign = "center";
        ctx.fillText(lbl, ox, y + 6);
        ctx.textAlign = "left";
      };
      drawOsc(50, x1(t), "#2563eb", "1");
      drawOsc(100, x2(t), "#dc2626", "2");
      ctx.fillStyle = "#64748b";
      ctx.font = "11px sans-serif";
      ctx.textAlign = "center";
      ctx.fillText("2 con l\u1EAFc l\xF2 xo", 75, MIDY + SCALE3 + 28);
      ctx.textAlign = "left";
      const X = (tau) => GX0 + tau / WINDOW * (GX1 - GX0);
      const Y = (val) => MIDY - val * SCALE3;
      ctx.strokeStyle = "#e6ebf1";
      ctx.lineWidth = 1;
      const per = 2 * Math.PI / OMEGA;
      for (let tau = per / 2; tau < WINDOW - 1e-3; tau += per / 2) {
        ctx.beginPath();
        ctx.moveTo(X(tau), 18);
        ctx.lineTo(X(tau), H5 - 22);
        ctx.stroke();
      }
      for (const lv of [1.5, 1, 0.5, -0.5, -1, -1.5]) {
        const gy = Y(lv);
        if (gy > 18 && gy < H5 - 22) {
          ctx.beginPath();
          ctx.moveTo(GX0, gy);
          ctx.lineTo(GX1, gy);
          ctx.stroke();
        }
      }
      ctx.strokeStyle = "#1e293b";
      ctx.fillStyle = "#1e293b";
      ctx.lineWidth = 1.8;
      ctx.lineJoin = "round";
      ctx.beginPath();
      ctx.moveTo(GX0, 18);
      ctx.lineTo(GX0, H5 - 22);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(GX0, MIDY);
      ctx.lineTo(GX1, MIDY);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(GX0, 12);
      ctx.lineTo(GX0 - 4, 20);
      ctx.lineTo(GX0 + 4, 20);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(GX1 + 6, MIDY);
      ctx.lineTo(GX1 - 3, MIDY - 4);
      ctx.lineTo(GX1 - 3, MIDY + 4);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("x", GX0 + 4, 16);
      ctx.fillText("t", GX1 - 2, MIDY - 6);
      const STEPS = 240;
      const iMax = STEPS;
      const curve = (f, color) => {
        ctx.strokeStyle = color;
        ctx.lineWidth = 2.4;
        ctx.beginPath();
        for (let i = 0; i <= iMax; i++) {
          const tau = i / STEPS * WINDOW;
          const px2 = X(tau), py = Y(f(tau));
          i === 0 ? ctx.moveTo(px2, py) : ctx.lineTo(px2, py);
        }
        ctx.stroke();
      };
      curve((tau) => refs2.current.a1 * Math.cos(OMEGA * tau + refs2.current.phi1), "#2563eb");
      curve((tau) => refs2.current.a2 * Math.cos(OMEGA * tau + refs2.current.phi2), "#dc2626");
      ctx.strokeStyle = "rgba(100,116,139,0.5)";
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(X(tc), 18);
      ctx.lineTo(X(tc), H5 - 22);
      ctx.stroke();
      const v1 = refs2.current.a1 * Math.cos(OMEGA * tc + refs2.current.phi1), v2 = refs2.current.a2 * Math.cos(OMEGA * tc + refs2.current.phi2);
      ctx.fillStyle = "#2563eb";
      ctx.beginPath();
      ctx.arc(X(tc), Y(v1), 4.5, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = "#dc2626";
      ctx.beginPath();
      ctx.arc(X(tc), Y(v2), 4.5, 0, Math.PI * 2);
      ctx.fill();
      ctx.font = "bold 11px sans-serif";
      ctx.fillStyle = "#2563eb";
      ctx.fillText("\u25A0 x\u2081", GX1 - 96, 16);
      ctx.fillStyle = "#dc2626";
      ctx.fillText("\u25A0 x\u2082", GX1 - 50, 16);
    }, []);
    const tick = (0, import_react16.useCallback)((ts) => {
      const dt = lastTs.current == null ? 0 : Math.min(0.05, (ts - lastTs.current) / 1e3);
      lastTs.current = ts;
      if (refs2.current.running)
        tRef.current += dt;
      draw();
      raf.current = requestAnimationFrame(tick);
    }, [draw]);
    (0, import_react16.useEffect)(() => {
      raf.current = requestAnimationFrame(tick);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [tick]);
    let dphi = (phi2 ?? 0) - (phi1 ?? 0);
    dphi = Math.atan2(Math.sin(dphi), Math.cos(dphi));
    const ad = Math.abs(dphi);
    const rel = ad < 0.04 ? "c\xF9ng pha" : Math.abs(ad - Math.PI) < 0.04 ? "ng\u01B0\u1EE3c pha" : Math.abs(ad - Math.PI / 2) < 0.04 ? "vu\xF4ng pha" : dphi > 0 ? "dao \u0111\u1ED9ng 2 s\u1EDBm pha h\u01A1n" : "dao \u0111\u1ED9ng 2 tr\u1EC5 pha h\u01A1n";
    const inputErr = phi1 === null || phi2 === null;
    return /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("canvas", { ref: cv, width: W5, height: H5, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("div", { className: "flex items-center justify-center gap-3 my-2 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("span", { className: "text-gray-600", children: "\u0110\u1ED9 l\u1EC7ch pha \u0394\u03C6 = \u03C6\u2082 \u2212 \u03C6\u2081 =" }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("b", { className: "text-indigo-700", children: phi1 !== null && phi2 !== null ? `${dphi.toFixed(2)} rad (${fmtPi(dphi)})` : "\u2014" }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("span", { className: "px-2 py-0.5 rounded-full bg-indigo-100 text-indigo-700 font-bold", children: phi1 !== null && phi2 !== null ? rel : "pha kh\xF4ng h\u1EE3p l\u1EC7" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("div", { className: "grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-2 mt-2 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("span", { className: "w-24 text-blue-700 font-semibold", children: "A\u2081" }),
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("input", { type: "range", min: 0.2, max: 1.4, step: 0.1, value: a1, onChange: (e) => {
              touch();
              setA1(Number(e.target.value));
            }, className: "flex-1 accent-blue-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("span", { className: "w-10 text-right font-semibold", children: a1.toFixed(1) })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("span", { className: "w-24 text-red-700 font-semibold", children: "A\u2082" }),
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("input", { type: "range", min: 0.2, max: 1.4, step: 0.1, value: a2, onChange: (e) => {
              touch();
              setA2(Number(e.target.value));
            }, className: "flex-1 accent-red-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("span", { className: "w-10 text-right font-semibold", children: a2.toFixed(1) })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("span", { className: "w-24 text-blue-700 font-semibold", children: "\u03C6\u2081 (rad)" }),
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("input", { type: "text", value: p1s, onChange: (e) => {
              touch();
              setP1s(e.target.value);
            }, placeholder: "vd: 0", className: `flex-1 px-2 py-1 rounded-lg border bg-white ${phi1 === null ? "border-red-400" : "border-gray-200"}` })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("span", { className: "w-24 text-red-700 font-semibold", children: "\u03C6\u2082 (rad)" }),
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("input", { type: "text", value: p2s, onChange: (e) => {
              touch();
              setP2s(e.target.value);
            }, placeholder: "vd: pi/2", className: `flex-1 px-2 py-1 rounded-lg border bg-white ${phi2 === null ? "border-red-400" : "border-gray-200"}` })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("div", { className: "phase-presets flex flex-wrap gap-2 mt-2", children: [
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("button", { onClick: () => {
            setP2s(String(phi1 ?? 0));
            touch();
          }, children: "C\xF9ng pha" }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("button", { onClick: () => {
            setP2s(String((phi1 ?? 0) + Math.PI / 2));
            touch();
          }, children: "Vu\xF4ng pha" }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("button", { onClick: () => {
            setP2s(String((phi1 ?? 0) + Math.PI));
            touch();
          }, children: "Ng\u01B0\u1EE3c pha" })
        ] }),
        inputErr && /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("p", { className: "text-[11px] text-red-500 mt-1", children: "Pha kh\xF4ng h\u1EE3p l\u1EC7 \u2014 nh\u1EADp s\u1ED1 ho\u1EB7c bi\u1EC3u th\u1EE9c radian ch\u1EE9a pi, vd: pi/2, -pi/3, 2*pi/3." }),
        /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("button", { onClick: () => {
            touch();
            setRunning((r) => !r);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-indigo-600 text-white font-bold hover:bg-indigo-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)(import_jsx_runtime13.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)(import_jsx_runtime13.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime13.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("button", { onClick: () => {
            tRef.current = 0;
          }, className: "px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: "V\u1EC1 t = 0" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime13.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("h4", { className: "font-bold text-indigo-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime13.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "C\xF9ng pha: \u0394\u03C6 = 0; ng\u01B0\u1EE3c pha: \u0394\u03C6 = \xB1\u03C0; vu\xF4ng pha: \u0394\u03C6 = \xB1\u03C0/2. Hai v\u1EADt b\xEAn tr\xE1i dao \u0111\u1ED9ng theo th\u1EDDi gian th\u1EF1c; con tr\u1ECF tr\xEAn \u0111\u1ED3 th\u1ECB cho th\u1EA5y \u0111\u1ED9 l\u1EC7ch ngang gi\u1EEFa hai \u0111\u01B0\u1EDDng = \u0111\u1ED9 l\u1EC7ch pha." })
        ] })
      ] })
    ] });
  };
  var TwoOscillationsSimulation_default = TwoOscillationsSimulation;

  // physics/vendor/simulations/DrivenResonanceSimulation.tsx
  var import_react17 = __toESM(require_react());
  var import_jsx_runtime14 = __toESM(require_jsx_runtime());
  var W6 = 620;
  var H6 = 170;
  var GW3 = 620;
  var GH3 = 190;
  var W0 = 1;
  var amp = (w, b) => 1 / Math.sqrt((W0 * W0 - w * w) ** 2 + (2 * b * w) ** 2);
  var DrivenResonanceSimulation = ({ title = "Dao \u0111\u1ED9ng c\u01B0\u1EE1ng b\u1EE9c & c\u1ED9ng h\u01B0\u1EDFng", description = "T\u1EA7n s\u1ED1 l\u1EF1c c\u01B0\u1EE1ng b\u1EE9c \u03C9 ti\u1EBFn t\u1EDBi t\u1EA7n s\u1ED1 ri\xEAng \u03C9\u2080 \u2192 bi\xEAn \u0111\u1ED9 t\u0103ng v\u1ECDt (c\u1ED9ng h\u01B0\u1EDFng). Ma s\xE1t (t\u1EAFt d\u1EA7n) c\xE0ng l\u1EDBn, \u0111\u1EC9nh c\u1ED9ng h\u01B0\u1EDFng c\xE0ng th\u1EA5p v\xE0 t\xF9.", onComplete, onAction }) => {
    const top = useSimRef("DrivenResonanceSimulation.top", null);
    const gcv = useSimRef("DrivenResonanceSimulation.gcv", null);
    const raf = useSimRef("DrivenResonanceSimulation.raf", null);
    const tRef = useSimRef("DrivenResonanceSimulation.tRef", 0);
    const [wf, setWf] = useSimState("DrivenResonanceSimulation.wf", 0.7);
    const [beta, setBeta] = useSimState("DrivenResonanceSimulation.beta", 0.12);
    const [running2, setRunning] = useSimState("DrivenResonanceSimulation.running", true);
    const wfRef = useSimRef("DrivenResonanceSimulation.wfRef", wf), betaRef = useSimRef("DrivenResonanceSimulation.betaRef", beta), runRef = useSimRef("DrivenResonanceSimulation.runRef", running2);
    const firstRef = useSimRef("DrivenResonanceSimulation.firstRef", false);
    (0, import_react17.useEffect)(() => {
      wfRef.current = wf;
    }, [wf]);
    (0, import_react17.useEffect)(() => {
      betaRef.current = beta;
    }, [beta]);
    (0, import_react17.useEffect)(() => {
      runRef.current = running2;
    }, [running2]);
    const touch = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const draw = (0, import_react17.useCallback)(() => {
      const ctx = prepCanvas(top.current, W6, H6);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W6, H6);
      const b = betaRef.current, w = wfRef.current;
      const A = amp(w, b), Apk = amp(W0, b);
      const disp = A / Apk * 78;
      const cx = W6 / 2, cy = 72;
      const x = cx + disp * Math.cos(tRef.current * w);
      ctx.fillStyle = "#e2e8f0";
      ctx.fillRect(60, cy + 22, W6 - 120, 5);
      ctx.strokeStyle = "#cbd5e1";
      ctx.setLineDash([4, 4]);
      ctx.beginPath();
      ctx.moveTo(cx, cy - 34);
      ctx.lineTo(cx, cy + 30);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.strokeStyle = "#94a3b8";
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(60, cy);
      const n = 16;
      for (let i = 1; i < n; i++) {
        const px2 = 60 + (x - 20 - 60) * (i / n);
        ctx.lineTo(px2, cy + (i % 2 ? -9 : 9));
      }
      ctx.lineTo(x - 20, cy);
      ctx.stroke();
      ctx.fillStyle = "#1e293b";
      ctx.fillRect(56, cy - 26, 6, 52);
      const g = ctx.createLinearGradient(x - 20, cy - 20, x + 20, cy + 20);
      g.addColorStop(0, "#60a5fa");
      g.addColorStop(1, "#1d4ed8");
      ctx.fillStyle = g;
      ctx.fillRect(x - 20, cy - 20, 40, 40);
      ctx.fillStyle = "#334155";
      ctx.font = "bold 13px sans-serif";
      ctx.fillText(`\u03C9 = ${w.toFixed(2)}   \xB7   A \u2248 ${(A / Apk).toFixed(2)} (so v\u1EDBi c\u1ED9ng h\u01B0\u1EDFng)`, 14, H6 - 14);
      if (Math.abs(w - W0) < 0.06) {
        ctx.fillStyle = "#dc2626";
        ctx.font = "bold 16px sans-serif";
        ctx.fillText("C\u1ED8NG H\u01AF\u1EDENG!", cx - 56, 24);
      }
    }, []);
    const drawGraph = (0, import_react17.useCallback)(() => {
      const ctx = prepCanvas(gcv.current, GW3, GH3);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, GW3, GH3);
      const b = betaRef.current, x0 = 46, y0 = GH3 - 26, plotW = GW3 - x0 - 16, plotH = GH3 - 44, peak = amp(W0, b), wmax = 2.2;
      const X = (w2) => x0 + w2 / wmax * plotW;
      const Y = (a) => y0 - a / peak * plotH * 0.92;
      ctx.strokeStyle = "#eef2f7";
      ctx.lineWidth = 1;
      ctx.fillStyle = "#94a3b8";
      ctx.font = "10px sans-serif";
      ctx.textAlign = "center";
      for (let w2 = 0.5; w2 <= wmax; w2 += 0.5) {
        ctx.beginPath();
        ctx.moveTo(X(w2), 12);
        ctx.lineTo(X(w2), y0);
        ctx.stroke();
        ctx.fillText(w2.toFixed(1), X(w2), y0 + 14);
      }
      ctx.textAlign = "left";
      ctx.strokeStyle = "#1e293b";
      ctx.fillStyle = "#1e293b";
      ctx.lineWidth = 2;
      ctx.lineJoin = "round";
      ctx.beginPath();
      ctx.moveTo(x0, 12);
      ctx.lineTo(x0, y0);
      ctx.lineTo(x0 + plotW, y0);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x0, 6);
      ctx.lineTo(x0 - 4, 14);
      ctx.lineTo(x0 + 4, 14);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(x0 + plotW + 6, y0);
      ctx.lineTo(x0 + plotW - 3, y0 - 4);
      ctx.lineTo(x0 + plotW - 3, y0 + 4);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("A", x0 - 2, 11);
      ctx.fillText("\u03C9", x0 + plotW + 2, y0 + 16);
      ctx.strokeStyle = "rgba(220,38,38,.45)";
      ctx.setLineDash([4, 4]);
      ctx.beginPath();
      ctx.moveTo(X(W0), 12);
      ctx.lineTo(X(W0), y0);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#dc2626";
      ctx.font = "12px sans-serif";
      ctx.fillText("\u03C9\u2080", X(W0) - 8, 22);
      ctx.strokeStyle = "#7c3aed";
      ctx.lineWidth = 2.5;
      ctx.beginPath();
      for (let i = 0; i <= 170; i++) {
        const w2 = i / 170 * wmax;
        const px2 = X(w2), py = Y(amp(w2, b));
        i === 0 ? ctx.moveTo(px2, py) : ctx.lineTo(px2, py);
      }
      ctx.stroke();
      const w = wfRef.current;
      ctx.fillStyle = "#16a34a";
      ctx.beginPath();
      ctx.arc(X(w), Y(amp(w, b)), 5, 0, Math.PI * 2);
      ctx.fill();
    }, []);
    const tick = (0, import_react17.useCallback)(() => {
      if (runRef.current)
        tRef.current += 0.05;
      draw();
      drawGraph();
      raf.current = requestAnimationFrame(tick);
    }, [draw, drawGraph]);
    (0, import_react17.useEffect)(() => {
      raf.current = requestAnimationFrame(tick);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [tick]);
    return /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)("div", { className: "sim-visuals", children: [
        /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("canvas", { ref: top, width: W6, height: H6, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }),
        /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("canvas", { ref: gcv, width: GW3, height: GH3, className: "w-full mt-2 rounded-lg border border-gray-100 bg-slate-50" })
      ] }),
      /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)("div", { className: "grid grid-cols-1 md:grid-cols-2 gap-x-4 gap-y-2 mt-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("span", { className: "w-32 text-gray-600", children: "T\u1EA7n s\u1ED1 c\u01B0\u1EE1ng b\u1EE9c \u03C9" }),
            /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("input", { type: "range", min: 0.2, max: 2, step: 0.02, value: wf, onChange: (e) => {
              touch();
              setWf(Number(e.target.value));
            }, onPointerUp: () => onAction?.({ action: "frequency_changed", parameters: { frequency: wf, damping: beta } }), onKeyUp: () => onAction?.({ action: "frequency_changed", parameters: { frequency: wf, damping: beta } }), className: "flex-1 accent-violet-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("span", { className: "w-10 text-right font-semibold", children: wf.toFixed(2) })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("span", { className: "w-32 text-gray-600", children: "Ma s\xE1t (t\u1EAFt d\u1EA7n) \u03B2" }),
            /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("input", { type: "range", min: 0.04, max: 0.6, step: 0.02, value: beta, onChange: (e) => {
              touch();
              setBeta(Number(e.target.value));
            }, onPointerUp: () => onAction?.({ action: "damping_changed", parameters: { frequency: wf, damping: beta } }), onKeyUp: () => onAction?.({ action: "damping_changed", parameters: { frequency: wf, damping: beta } }), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("span", { className: "w-10 text-right font-semibold", children: beta.toFixed(2) })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("button", { onClick: () => {
            touch();
            setRunning((r) => {
              const next = !r;
              onAction?.({ action: next ? "played" : "paused", parameters: { frequency: wf, damping: beta } });
              return next;
            });
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-violet-600 text-white font-bold hover:bg-violet-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)(import_jsx_runtime14.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime14.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)(import_jsx_runtime14.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime14.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("button", { onClick: () => {
            touch();
            setWf(1);
            onAction?.({ action: "resonance_selected", parameters: { frequency: 1, damping: beta } });
          }, className: "px-4 py-2 rounded-lg border border-red-200 text-red-600 font-bold hover:bg-red-50", children: "\u0110\u1EB7t \u03C9 = \u03C9\u2080 (c\u1ED9ng h\u01B0\u1EDFng)" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime14.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("h4", { className: "font-bold text-violet-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime14.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "C\u1ED9ng h\u01B0\u1EDFng: \u03C9 = \u03C9\u2080 \u2192 bi\xEAn \u0111\u1ED9 l\u1EDBn nh\u1EA5t. Gi\u1EA3m ma s\xE1t \u2192 \u0111\u1EC9nh cao & nh\u1ECDn; t\u0103ng ma s\xE1t \u2192 \u0111\u1EC9nh th\u1EA5p & t\xF9 (dao \u0111\u1ED9ng t\u1EAFt d\u1EA7n nhanh)." })
        ] })
      ] })
    ] });
  };
  var DrivenResonanceSimulation_default = DrivenResonanceSimulation;

  // physics/vendor/simulations/WavePropagationSimulation.tsx
  var import_react18 = __toESM(require_react());
  var import_jsx_runtime15 = __toESM(require_jsx_runtime());
  var WavePropagationSimulation = ({ title = "M\xF4 t\u1EA3 v\xE0 s\u1EF1 truy\u1EC1n s\xF3ng", description = "S\xF3ng ngang lan truy\u1EC1n: m\u1ED7i ph\u1EA7n t\u1EED dao \u0111\u1ED9ng t\u1EA1i ch\u1ED7, pha dao \u0111\u1ED9ng truy\u1EC1n \u0111i. Quan s\xE1t \u03BB, T, v = \u03BB\xB7f.", onComplete }) => {
    const cv = useSimRef("WavePropagationSimulation.cv", null);
    const raf = useSimRef("WavePropagationSimulation.raf", null);
    const tRef = useSimRef("WavePropagationSimulation.tRef", 0);
    const lastRef = useSimRef("WavePropagationSimulation.lastRef", null);
    const [running2, setRunning] = useSimState("WavePropagationSimulation.running", true);
    const [A, setA] = useSimState("WavePropagationSimulation.A", 40);
    const [lambda, setLambda] = useSimState("WavePropagationSimulation.lambda", 160);
    const [f, setF] = useSimState("WavePropagationSimulation.f", 0.5);
    const firstRef = useSimRef("WavePropagationSimulation.firstRef", false);
    const W12 = 640, H12 = 240;
    const draw = (0, import_react18.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      ctx.lineWidth = 1;
      for (let gx = 0; gx <= W12; gx += 40) {
        ctx.strokeStyle = gx % 80 === 0 ? "#dbe4ee" : "#eef2f7";
        ctx.beginPath();
        ctx.moveTo(gx, 0);
        ctx.lineTo(gx, H12);
        ctx.stroke();
      }
      for (let gy = 0; gy <= H12; gy += 40) {
        ctx.strokeStyle = "#eef2f7";
        ctx.beginPath();
        ctx.moveTo(0, gy);
        ctx.lineTo(W12, gy);
        ctx.stroke();
      }
      const y0 = H12 / 2;
      const k = 2 * Math.PI / lambda;
      const w = 2 * Math.PI * f;
      const t = tRef.current;
      ctx.strokeStyle = "#e2e8f0";
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(0, y0);
      ctx.lineTo(W12, y0);
      ctx.stroke();
      ctx.strokeStyle = "#0ea5e9";
      ctx.lineWidth = 2.5;
      ctx.beginPath();
      for (let x = 0; x <= W12; x += 2) {
        const y = y0 - A * Math.sin(k * x - w * t);
        x === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
      }
      ctx.stroke();
      for (let i = 0; i < W12; i += Math.round(lambda / 4)) {
        const y = y0 - A * Math.sin(k * i - w * t);
        ctx.beginPath();
        ctx.arc(i, y, 4, 0, Math.PI * 2);
        ctx.fillStyle = "#ef4444";
        ctx.fill();
      }
      ctx.strokeStyle = "rgba(100,116,139,0.6)";
      ctx.setLineDash([4, 3]);
      ctx.beginPath();
      ctx.moveTo(20, 24);
      ctx.lineTo(20 + lambda, 24);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#475569";
      ctx.font = "12px sans-serif";
      ctx.fillText("\u03BB", 20 + lambda / 2 - 4, 18);
    }, [A, lambda, f]);
    const tick = (0, import_react18.useCallback)((ts) => {
      if (lastRef.current == null)
        lastRef.current = ts;
      const dt = Math.min(0.05, (ts - lastRef.current) / 1e3);
      lastRef.current = ts;
      tRef.current += dt;
      draw();
      raf.current = requestAnimationFrame(tick);
    }, [draw]);
    (0, import_react18.useEffect)(() => {
      if (running2) {
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        lastRef.current = null;
        raf.current = requestAnimationFrame(tick);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, tick, onComplete]);
    (0, import_react18.useEffect)(() => {
      draw();
    }, [draw]);
    const v = (lambda * f).toFixed(0);
    const T2 = (1 / f).toFixed(2);
    return /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("canvas", { ref: cv, width: W12, height: H12, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "grid grid-cols-3 gap-2 my-3 text-center", children: [
          /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "p-2 rounded-lg bg-sky-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "text-lg font-black text-sky-700", children: [
              v,
              " px/s"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("div", { className: "text-[11px] text-gray-500", children: "T\u1ED1c \u0111\u1ED9 v = \u03BB\xB7f" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "p-2 rounded-lg bg-violet-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "text-lg font-black text-violet-700", children: [
              T2,
              " s"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("div", { className: "text-[11px] text-gray-500", children: "Chu k\xEC T = 1/f" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "p-2 rounded-lg bg-amber-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "text-lg font-black text-amber-700", children: [
              lambda,
              " px"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("div", { className: "text-[11px] text-gray-500", children: "B\u01B0\u1EDBc s\xF3ng \u03BB" })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "space-y-2", children: [
          /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("span", { className: "w-28 text-gray-600", children: "Bi\xEAn \u0111\u1ED9 A" }),
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("input", { type: "range", min: 10, max: 80, value: A, onChange: (e) => setA(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("span", { className: "w-12 text-right font-semibold", children: A })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("span", { className: "w-28 text-gray-600", children: "B\u01B0\u1EDBc s\xF3ng \u03BB" }),
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("input", { type: "range", min: 80, max: 320, step: 10, value: lambda, onChange: (e) => setLambda(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("span", { className: "w-12 text-right font-semibold", children: lambda })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("span", { className: "w-28 text-gray-600", children: "T\u1EA7n s\u1ED1 f" }),
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("input", { type: "range", min: 0.2, max: 2, step: 0.1, value: f, onChange: (e) => setF(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              f,
              "Hz"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("button", { onClick: () => setRunning((r) => !r), className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-sky-600 text-white font-bold hover:bg-sky-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)(import_jsx_runtime15.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)(import_jsx_runtime15.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("button", { onClick: () => {
            tRef.current = 0;
            draw();
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime15.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " \u0110\u1EB7t l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime15.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("h4", { className: "font-bold text-sky-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime15.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var WavePropagationSimulation_default = WavePropagationSimulation;

  // physics/vendor/simulations/WaveInterferenceSimulation.tsx
  var import_react19 = __toESM(require_react());
  var import_jsx_runtime16 = __toESM(require_jsx_runtime());
  var W7 = 560;
  var H7 = 320;
  var CELL = 4;
  var SX = 90;
  var WOMEGA = 4;
  var WaveInterferenceSimulation = ({ title = "Giao thoa s\xF3ng hai ngu\u1ED3n", description = "Hai ngu\u1ED3n S\u2081, S\u2082 c\xF9ng pha t\u1EA1o v\xE2n giao thoa: c\u1EF1c \u0111\u1EA1i (s\xF3ng t\u0103ng c\u01B0\u1EDDng) khi hi\u1EC7u \u0111\u01B0\u1EDDng \u0111i |r\u2081\u2212r\u2082| = m\xB7\u03BB; c\u1EF1c ti\u1EC3u khi = (m+\xBD)\xB7\u03BB.", onComplete }) => {
    const cv = useSimRef("WaveInterferenceSimulation.cv", null);
    const raf = useSimRef("WaveInterferenceSimulation.raf", null);
    const tRef = useSimRef("WaveInterferenceSimulation.tRef", 0);
    const [dist, setDist] = useSimState("WaveInterferenceSimulation.dist", 120);
    const [lambda, setLambda] = useSimState("WaveInterferenceSimulation.lambda", 44);
    const [running2, setRunning] = useSimState("WaveInterferenceSimulation.running", true);
    const dRef = useSimRef("WaveInterferenceSimulation.dRef", dist), lRef = useSimRef("WaveInterferenceSimulation.lRef", lambda), runRef = useSimRef("WaveInterferenceSimulation.runRef", running2);
    const firstRef = useSimRef("WaveInterferenceSimulation.firstRef", false);
    (0, import_react19.useEffect)(() => {
      dRef.current = dist;
    }, [dist]);
    (0, import_react19.useEffect)(() => {
      lRef.current = lambda;
    }, [lambda]);
    (0, import_react19.useEffect)(() => {
      runRef.current = running2;
    }, [running2]);
    const touch = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const draw = (0, import_react19.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W7, H7);
      if (!ctx)
        return;
      const d = dRef.current, lam = lRef.current, t = tRef.current;
      const k = 2 * Math.PI / lam;
      const cy = H7 / 2, s1y = cy - d / 2, s2y = cy + d / 2;
      for (let gy = 0; gy < H7; gy += CELL)
        for (let gx = 0; gx < W7; gx += CELL) {
          const r1 = Math.hypot(gx - SX, gy - s1y), r2 = Math.hypot(gx - SX, gy - s2y);
          const u = Math.sin(k * r1 - WOMEGA * t) + Math.sin(k * r2 - WOMEGA * t);
          const f = (u / 2 + 1) / 2;
          const r = Math.round(15 + 175 * f), g = Math.round(40 + 185 * f), b = Math.round(90 + 165 * f);
          ctx.fillStyle = `rgb(${r},${g},${b})`;
          ctx.fillRect(gx, gy, CELL, CELL);
        }
      ctx.strokeStyle = "rgba(255,255,255,0.45)";
      ctx.lineWidth = 1;
      ctx.setLineDash([5, 4]);
      ctx.beginPath();
      ctx.moveTo(SX, cy);
      ctx.lineTo(W7, cy);
      ctx.stroke();
      ctx.setLineDash([]);
      for (const [sy, lbl] of [[s1y, "S\u2081"], [s2y, "S\u2082"]]) {
        ctx.fillStyle = "#dc2626";
        ctx.beginPath();
        ctx.arc(SX, sy, 6, 0, Math.PI * 2);
        ctx.fill();
        ctx.strokeStyle = "#fff";
        ctx.lineWidth = 2;
        ctx.stroke();
        ctx.fillStyle = "#fff";
        ctx.font = "bold 13px sans-serif";
        ctx.textAlign = "right";
        ctx.fillText(lbl, SX - 10, sy + 4);
        ctx.textAlign = "left";
      }
      ctx.fillStyle = "rgba(255,255,255,0.9)";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText(`d = ${d}   \xB7   \u03BB = ${lam}`, 12, 20);
    }, []);
    const tick = (0, import_react19.useCallback)(() => {
      if (runRef.current)
        tRef.current += 0.05;
      draw();
      raf.current = requestAnimationFrame(tick);
    }, [draw]);
    (0, import_react19.useEffect)(() => {
      raf.current = requestAnimationFrame(tick);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [tick]);
    return /* @__PURE__ */ (0, import_jsx_runtime16.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("canvas", { ref: cv, width: W7, height: H7, className: "w-full rounded-lg border border-gray-100" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime16.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime16.jsxs)("div", { className: "grid grid-cols-1 md:grid-cols-2 gap-x-4 gap-y-2 mt-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime16.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("span", { className: "w-36 text-gray-600", children: "Kho\u1EA3ng c\xE1ch 2 ngu\u1ED3n d" }),
            /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("input", { type: "range", min: 50, max: 220, step: 5, value: dist, onChange: (e) => {
              touch();
              setDist(Number(e.target.value));
            }, className: "flex-1 accent-blue-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("span", { className: "w-10 text-right font-semibold", children: dist })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime16.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("span", { className: "w-36 text-gray-600", children: "B\u01B0\u1EDBc s\xF3ng \u03BB" }),
            /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("input", { type: "range", min: 24, max: 72, step: 2, value: lambda, onChange: (e) => {
              touch();
              setLambda(Number(e.target.value));
            }, className: "flex-1 accent-blue-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("span", { className: "w-10 text-right font-semibold", children: lambda })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("div", { className: "flex gap-2 mt-3", children: /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("button", { onClick: () => {
          touch();
          setRunning((r) => !r);
        }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime16.jsxs)(import_jsx_runtime16.Fragment, { children: [
          /* @__PURE__ */ (0, import_jsx_runtime16.jsx)(Pause, { className: "w-4 h-4" }),
          " D\u1EEBng"
        ] }) : /* @__PURE__ */ (0, import_jsx_runtime16.jsxs)(import_jsx_runtime16.Fragment, { children: [
          /* @__PURE__ */ (0, import_jsx_runtime16.jsx)(Play, { className: "w-4 h-4" }),
          " Ch\u1EA1y"
        ] }) }) }),
        /* @__PURE__ */ (0, import_jsx_runtime16.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("h4", { className: "font-bold text-blue-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime16.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "V\xF9ng s\xE1ng = c\u1EF1c \u0111\u1EA1i giao thoa (2 s\xF3ng c\xF9ng pha t\u0103ng c\u01B0\u1EDDng); v\xF9ng t\u1ED1i xen k\u1EBD = c\u1EF1c ti\u1EC3u (ng\u01B0\u1EE3c pha tri\u1EC7t ti\xEAu). T\u0103ng \u03BB ho\u1EB7c gi\u1EA3m d \u2192 v\xE2n th\u01B0a h\u01A1n." })
        ] })
      ] })
    ] });
  };
  var WaveInterferenceSimulation_default = WaveInterferenceSimulation;

  // physics/vendor/simulations/StandingWaveSimulation.tsx
  var import_react20 = __toESM(require_react());
  var import_jsx_runtime17 = __toESM(require_jsx_runtime());
  var W8 = 660;
  var H8 = 285;
  var X0 = 40;
  var X1 = 620;
  var MID = 120;
  var AMP = 78;
  var StandingWaveSimulation = ({ title = "S\xF3ng d\u1EEBng tr\xEAn d\xE2y hai \u0111\u1EA7u c\u1ED1 \u0111\u1ECBnh", description = "Hai s\xF3ng ng\u01B0\u1EE3c chi\u1EC1u ch\u1ED3ng ch\u1EA5t t\u1EA1o s\xF3ng d\u1EEBng: n\xFAt (\u0111\u1EE9ng y\xEAn) v\xE0 b\u1EE5ng (dao \u0111\u1ED9ng c\u1EF1c \u0111\u1EA1i). S\u1ED1 b\xF3 n \u2192 \u03BB = 2L/n.", onComplete }) => {
    const cv = useSimRef("StandingWaveSimulation.cv", null);
    const raf = useSimRef("StandingWaveSimulation.raf", null);
    const tRef = useSimRef("StandingWaveSimulation.tRef", 0);
    const [n, setN] = useSimState("StandingWaveSimulation.n", 3);
    const [running2, setRunning] = useSimState("StandingWaveSimulation.running", true);
    const nRef = useSimRef("StandingWaveSimulation.nRef", n), runRef = useSimRef("StandingWaveSimulation.runRef", running2);
    const firstRef = useSimRef("StandingWaveSimulation.firstRef", false);
    (0, import_react20.useEffect)(() => {
      nRef.current = n;
    }, [n]);
    (0, import_react20.useEffect)(() => {
      runRef.current = running2;
    }, [running2]);
    const touch = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const draw = (0, import_react20.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W8, H8);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W8, H8);
      const L = X1 - X0, nn = nRef.current, t = tRef.current;
      const env = (x) => AMP * Math.sin(nn * Math.PI * (x - X0) / L);
      const y = (x) => MID - env(x) * Math.cos(t);
      ctx.fillStyle = "rgba(124,58,237,0.10)";
      ctx.beginPath();
      ctx.moveTo(X0, MID);
      for (let x = X0; x <= X1; x += 4)
        ctx.lineTo(x, MID - Math.abs(env(x)));
      for (let x = X1; x >= X0; x -= 4)
        ctx.lineTo(x, MID + Math.abs(env(x)));
      ctx.closePath();
      ctx.fill();
      ctx.strokeStyle = "#7c3aed";
      ctx.lineWidth = 3;
      ctx.beginPath();
      for (let x = X0; x <= X1; x += 2) {
        const py = y(x);
        x === X0 ? ctx.moveTo(x, py) : ctx.lineTo(x, py);
      }
      ctx.stroke();
      ctx.fillStyle = "#1e293b";
      ctx.fillRect(X0 - 6, MID - 30, 6, 60);
      ctx.fillRect(X1, MID - 30, 6, 60);
      for (let k = 0; k <= nn; k++) {
        const xn2 = X0 + k * L / nn;
        ctx.fillStyle = "#dc2626";
        ctx.beginPath();
        ctx.arc(xn2, MID, 4.5, 0, Math.PI * 2);
        ctx.fill();
      }
      for (let k = 0; k < nn; k++) {
        const xb2 = X0 + (k + 0.5) * L / nn;
        ctx.fillStyle = "#f59e0b";
        ctx.beginPath();
        ctx.arc(xb2, y(xb2), 4, 0, Math.PI * 2);
        ctx.fill();
      }
      const xb = X0 + 0.5 * L / nn, xn = Math.min(X1, X0 + L / nn);
      ctx.textAlign = "center";
      ctx.fillStyle = "#b45309";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("B\u1EE5ng", xb, MID - AMP - 8);
      ctx.fillStyle = "#dc2626";
      ctx.fillText("N\xFAt", xn, MID + 24);
      const a = X0, b = X0 + L / nn, yb = MID + AMP + 30;
      ctx.strokeStyle = "#334155";
      ctx.fillStyle = "#334155";
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(a, yb - 6);
      ctx.lineTo(a, yb + 6);
      ctx.moveTo(b, yb - 6);
      ctx.lineTo(b, yb + 6);
      ctx.moveTo(a, yb);
      ctx.lineTo(b, yb);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(a, yb);
      ctx.lineTo(a + 7, yb - 3);
      ctx.lineTo(a + 7, yb + 3);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(b, yb);
      ctx.lineTo(b - 7, yb - 3);
      ctx.lineTo(b - 7, yb + 3);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 13px sans-serif";
      ctx.fillText("\u03BB/2", (a + b) / 2, yb - 6);
      ctx.textAlign = "left";
      ctx.fillStyle = "#dc2626";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("\u25CF n\xFAt (\u0111\u1EE9ng y\xEAn)", X0, H8 - 30);
      ctx.fillStyle = "#f59e0b";
      ctx.fillText("\u25CF b\u1EE5ng (c\u1EF1c \u0111\u1EA1i)", X0 + 130, H8 - 30);
      ctx.fillStyle = "#334155";
      ctx.font = "bold 13px sans-serif";
      ctx.fillText(`S\u1ED1 b\xF3 n = ${nn}   \xB7   s\u1ED1 b\u1EE5ng = ${nn}   \xB7   s\u1ED1 n\xFAt = ${nn + 1}   \xB7   \u03BB = 2L/${nn}`, X0, H8 - 12);
    }, []);
    const tick = (0, import_react20.useCallback)(() => {
      if (runRef.current)
        tRef.current += 0.06;
      draw();
      raf.current = requestAnimationFrame(tick);
    }, [draw]);
    (0, import_react20.useEffect)(() => {
      raf.current = requestAnimationFrame(tick);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [tick]);
    return /* @__PURE__ */ (0, import_jsx_runtime17.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("canvas", { ref: cv, width: W8, height: H8, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime17.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime17.jsxs)("div", { className: "flex items-center gap-3 mt-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime17.jsxs)("label", { className: "flex items-center gap-2 flex-1", children: [
            /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("span", { className: "w-40 text-gray-600", children: "S\u1ED1 b\xF3 s\xF3ng n (ho\u1EA1 \xE2m)" }),
            /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("input", { type: "range", min: 1, max: 6, step: 1, value: n, onChange: (e) => {
              touch();
              setN(Number(e.target.value));
            }, className: "flex-1 accent-violet-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("span", { className: "w-8 text-right font-semibold", children: n })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("button", { onClick: () => {
            touch();
            setRunning((r) => !r);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-violet-600 text-white font-bold hover:bg-violet-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime17.jsxs)(import_jsx_runtime17.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime17.jsx)(Pause, { className: "w-4 h-4" }),
            " D\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime17.jsxs)(import_jsx_runtime17.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime17.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime17.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("h4", { className: "font-bold text-violet-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime17.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "\u0110i\u1EC1u ki\u1EC7n s\xF3ng d\u1EEBng 2 \u0111\u1EA7u c\u1ED1 \u0111\u1ECBnh: L = n\xB7\u03BB/2 (n = 1, 2, 3\u2026). Kho\u1EA3ng c\xE1ch 2 n\xFAt (ho\u1EB7c 2 b\u1EE5ng) li\xEAn ti\u1EBFp = \u03BB/2." })
        ] })
      ] })
    ] });
  };
  var StandingWaveSimulation_default = StandingWaveSimulation;

  // physics/vendor/simulations/CoulombForceSimulation.tsx
  var import_react21 = __toESM(require_react());
  var import_jsx_runtime18 = __toESM(require_jsx_runtime());
  var CoulombForceSimulation = ({ title = "L\u1EF1c t\u01B0\u01A1ng t\xE1c Coulomb", description = "F = k\xB7|q\u2081\xB7q\u2082|/r\xB2. K\xE9o \u0111i\u1EC7n t\xEDch \u0111\u1EC3 \u0111\u1ED5i kho\u1EA3ng c\xE1ch r; \u0111\u1ED5i d\u1EA5u/\u0111\u1ED9 l\u1EDBn \u0111i\u1EC7n t\xEDch \u0111\u1EC3 th\u1EA5y l\u1EF1c h\xFAt/\u0111\u1EA9y.", onComplete }) => {
    const cv = useSimRef("CoulombForceSimulation.cv", null);
    const p1 = useSimRef("CoulombForceSimulation.p1", { x: 180, y: 150 });
    const p2 = useSimRef("CoulombForceSimulation.p2", { x: 460, y: 150 });
    const drag = useSimRef("CoulombForceSimulation.drag", null);
    const [q1, setQ1] = useSimState("CoulombForceSimulation.q1", 3);
    const [q2, setQ2] = useSimState("CoulombForceSimulation.q2", -3);
    const [rVal, setRVal] = useSimState("CoulombForceSimulation.rVal", 280);
    const [, force] = (0, import_react21.useState)(0);
    const firstRef = useSimRef("CoulombForceSimulation.firstRef", false);
    const W12 = 640, H12 = 300;
    const K = 9e4;
    const arrow = (ctx, x, y, dx, dy, color) => {
      const len = Math.hypot(dx, dy);
      if (len < 1)
        return;
      const ex = x + dx, ey = y + dy;
      ctx.strokeStyle = color;
      ctx.fillStyle = color;
      ctx.lineWidth = 4;
      ctx.lineCap = "round";
      ctx.beginPath();
      ctx.moveTo(x, y);
      ctx.lineTo(ex, ey);
      ctx.stroke();
      const a = Math.atan2(dy, dx);
      ctx.beginPath();
      ctx.moveTo(ex, ey);
      ctx.lineTo(ex - 14 * Math.cos(a - 0.42), ey - 14 * Math.sin(a - 0.42));
      ctx.lineTo(ex - 14 * Math.cos(a + 0.42), ey - 14 * Math.sin(a + 0.42));
      ctx.closePath();
      ctx.fill();
    };
    const sphere3 = (ctx, x, y, r, base, edge) => {
      const g = ctx.createRadialGradient(x - r * 0.35, y - r * 0.35, r * 0.12, x, y, r);
      g.addColorStop(0, "#ffffff");
      g.addColorStop(0.4, base);
      g.addColorStop(1, edge);
      ctx.fillStyle = g;
      ctx.beginPath();
      ctx.arc(x, y, r, 0, Math.PI * 2);
      ctx.fill();
    };
    const draw = (0, import_react21.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      const a = p1.current, b = p2.current;
      ctx.strokeStyle = "rgba(100,116,139,0.55)";
      ctx.lineWidth = 1.5;
      ctx.setLineDash([7, 5]);
      ctx.beginPath();
      ctx.moveTo(a.x, a.y);
      ctx.lineTo(b.x, b.y);
      ctx.stroke();
      ctx.setLineDash([]);
      const dx = b.x - a.x, dy = b.y - a.y;
      const r = Math.max(30, Math.hypot(dx, dy));
      const F = K * Math.abs(q1 * q2) / (r * r);
      const ux = dx / r, uy = dy / r;
      const attract = q1 * q2 < 0;
      const fl = Math.min(175, 36 + F * 1.15);
      const s1 = attract ? 1 : -1;
      arrow(ctx, a.x, a.y, ux * fl * s1, uy * fl * s1, "#7c3aed");
      arrow(ctx, b.x, b.y, -ux * fl * s1, -uy * fl * s1, "#7c3aed");
      const r1 = 16 + Math.abs(q1) * 2, r2 = 16 + Math.abs(q2) * 2;
      sphere3(ctx, a.x, a.y, r1, q1 >= 0 ? "#f87171" : "#60a5fa", q1 >= 0 ? "#b91c1c" : "#1d4ed8");
      sphere3(ctx, b.x, b.y, r2, q2 >= 0 ? "#f87171" : "#60a5fa", q2 >= 0 ? "#b91c1c" : "#1d4ed8");
      ctx.fillStyle = "#fff";
      ctx.font = "bold 16px sans-serif";
      ctx.textAlign = "center";
      ctx.fillText(q1 >= 0 ? "+" : "\u2212", a.x, a.y + 6);
      ctx.fillText(q2 >= 0 ? "+" : "\u2212", b.x, b.y + 6);
      ctx.fillStyle = "#334155";
      ctx.font = "bold 15px sans-serif";
      ctx.fillText("q\u2081", a.x, a.y - r1 - 8);
      ctx.fillText("q\u2082", b.x, b.y - r2 - 8);
      ctx.textAlign = "left";
      ctx.fillStyle = "#334155";
      ctx.font = "bold 13px sans-serif";
      ctx.fillText(`r = ${r.toFixed(0)}   F \u2248 ${F.toFixed(1)} (\u0111v)   ${attract ? "H\xDAT" : "\u0110\u1EA8Y"}`, 16, 24);
    }, [q1, q2, K]);
    (0, import_react21.useEffect)(() => {
      draw();
    }, [draw]);
    (0, import_react21.useEffect)(() => {
      const cx = W12 / 2, cy = H12 / 2;
      p1.current = { x: cx - rVal / 2, y: cy };
      p2.current = { x: cx + rVal / 2, y: cy };
      force((n) => n + 1);
      draw();
    }, [rVal, draw]);
    const pos = (e) => {
      const rect = cv.current.getBoundingClientRect();
      return { x: (e.clientX - rect.left) * (W12 / rect.width), y: (e.clientY - rect.top) * (H12 / rect.height) };
    };
    const down = (e) => {
      const p = pos(e);
      if (Math.hypot(p.x - p1.current.x, p.y - p1.current.y) < 30)
        drag.current = 1;
      else if (Math.hypot(p.x - p2.current.x, p.y - p2.current.y) < 30)
        drag.current = 2;
      if (drag.current && !firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const move = (e) => {
      if (!drag.current)
        return;
      const p = pos(e);
      const t = drag.current === 1 ? p1 : p2;
      t.current = { x: Math.max(20, Math.min(W12 - 20, p.x)), y: Math.max(20, Math.min(H12 - 20, p.y)) };
      force((n) => n + 1);
      draw();
    };
    const up = () => {
      drag.current = null;
    };
    return /* @__PURE__ */ (0, import_jsx_runtime18.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("canvas", { ref: cv, width: W12, height: H12, onPointerDown: down, onPointerMove: move, onPointerUp: up, onPointerLeave: up, className: "w-full rounded-lg border border-gray-100 bg-slate-50 touch-none cursor-grab" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime18.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime18.jsxs)("div", { className: "grid grid-cols-2 gap-3 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime18.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("span", { className: "w-16 text-gray-600", children: "q\u2081" }),
            /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("input", { type: "range", min: -6, max: 6, step: 1, value: q1, onChange: (e) => setQ1(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime18.jsxs)("span", { className: "w-10 text-right font-semibold", children: [
              q1 > 0 ? "+" : "",
              q1
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime18.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("span", { className: "w-16 text-gray-600", children: "q\u2082" }),
            /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("input", { type: "range", min: -6, max: 6, step: 1, value: q2, onChange: (e) => setQ2(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime18.jsxs)("span", { className: "w-10 text-right font-semibold", children: [
              q2 > 0 ? "+" : "",
              q2
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime18.jsxs)("label", { className: "flex items-center gap-2 text-sm mt-2", children: [
          /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("span", { className: "w-32 text-gray-600", children: "Kho\u1EA3ng c\xE1ch r" }),
          /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("input", { type: "range", min: 90, max: 460, step: 10, value: rVal, onChange: (e) => {
            firstRef.current || (firstRef.current = true, onComplete?.());
            setRVal(Number(e.target.value));
          }, className: "flex-1 accent-violet-500" }),
          /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("span", { className: "w-12 text-right font-semibold", children: rVal })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime18.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("h4", { className: "font-bold text-violet-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime18.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var CoulombForceSimulation_default = CoulombForceSimulation;

  // physics/vendor/simulations/ElectricFieldSimulation.tsx
  var import_react22 = __toESM(require_react());
  var import_jsx_runtime19 = __toESM(require_jsx_runtime());
  var PX0 = 64;
  var PX1 = 560;
  var TOP = 46;
  var BOT = 286;
  var START_X = PX0 + 30;
  var START_Y = (TOP + BOT) / 2;
  var ElectricFieldSimulation = ({ title = "\u0110i\u1EC7n tr\u01B0\u1EDDng \u0111\u1EC1u gi\u1EEFa hai b\u1EA3n", description = "B\u1EA3n + \u1EDF tr\xEAn, b\u1EA3n \u2212 \u1EDF d\u01B0\u1EDBi; E\u20D7 h\u01B0\u1EDBng xu\u1ED1ng. \u0110i\u1EC7n t\xEDch ch\u1ECBu l\u1EF1c F = qE. Ch\u1EC9nh h\u01B0\u1EDBng v\u2080: d\u1ECDc E \u2192 qu\u1EF9 \u0111\u1EA1o th\u1EB3ng; xi\xEAn \u2192 parabol.", onComplete }) => {
    const cv = useSimRef("ElectricFieldSimulation.cv", null);
    const raf = useSimRef("ElectricFieldSimulation.raf", null);
    const lastRef = useSimRef("ElectricFieldSimulation.lastRef", null);
    const p = useSimRef("ElectricFieldSimulation.p", { x: START_X, y: START_Y });
    const v = useSimRef("ElectricFieldSimulation.v", { x: 0, y: 0 });
    const trail = useSimRef("ElectricFieldSimulation.trail", []);
    const [E, setE] = useSimState("ElectricFieldSimulation.E", 3);
    const [q, setQ] = useSimState("ElectricFieldSimulation.q", 2);
    const [v0, setV0] = useSimState("ElectricFieldSimulation.v0", 140);
    const [angle2, setAngle] = useSimState("ElectricFieldSimulation.angle", 0);
    const [running2, setRunning] = useSimState("ElectricFieldSimulation.running", false);
    const [, force] = (0, import_react22.useState)(0);
    const firstRef = useSimRef("ElectricFieldSimulation.firstRef", false);
    const W12 = 600, H12 = 330;
    const reset = (0, import_react22.useCallback)(() => {
      setRunning(false);
      p.current = { x: START_X, y: START_Y };
      v.current = { x: 0, y: 0 };
      trail.current = [];
      force((n) => n + 1);
    }, []);
    (0, import_react22.useEffect)(() => {
      reset();
    }, [reset]);
    const arrow = (ctx, x, y, dx, dy, color, lw = 3) => {
      const len = Math.hypot(dx, dy);
      if (len < 2)
        return;
      const ex = x + dx, ey = y + dy, a = Math.atan2(dy, dx);
      ctx.strokeStyle = color;
      ctx.fillStyle = color;
      ctx.lineWidth = lw;
      ctx.lineCap = "round";
      ctx.beginPath();
      ctx.moveTo(x, y);
      ctx.lineTo(ex, ey);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(ex, ey);
      ctx.lineTo(ex - 11 * Math.cos(a - 0.42), ey - 11 * Math.sin(a - 0.42));
      ctx.lineTo(ex - 11 * Math.cos(a + 0.42), ey - 11 * Math.sin(a + 0.42));
      ctx.closePath();
      ctx.fill();
      ctx.lineCap = "butt";
    };
    const vecLabel = (ctx, txt, x, y, color) => {
      ctx.fillStyle = color;
      ctx.font = "bold italic 16px sans-serif";
      ctx.textAlign = "left";
      const w = ctx.measureText(txt).width;
      ctx.fillText(txt, x, y);
      ctx.strokeStyle = color;
      ctx.fillStyle = color;
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(x - 1, y - 15);
      ctx.lineTo(x + w + 1, y - 15);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x + w + 1, y - 15);
      ctx.lineTo(x + w - 3, y - 18);
      ctx.lineTo(x + w - 3, y - 12);
      ctx.closePath();
      ctx.fill();
    };
    const draw = (0, import_react22.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.fillStyle = "#ef4444";
      ctx.fillRect(PX0, TOP - 6, PX1 - PX0, 10);
      ctx.fillStyle = "#3b82f6";
      ctx.fillRect(PX0, BOT - 4, PX1 - PX0, 10);
      ctx.fillStyle = "#b91c1c";
      ctx.font = "bold 16px sans-serif";
      ctx.textAlign = "left";
      for (let x2 = PX0 + 18; x2 < PX1; x2 += 46)
        ctx.fillText("+", x2, TOP - 10);
      ctx.fillStyle = "#1d4ed8";
      for (let x2 = PX0 + 18; x2 < PX1; x2 += 46)
        ctx.fillText("\u2212", x2, BOT + 22);
      ctx.strokeStyle = "rgba(124,58,237,0.45)";
      ctx.fillStyle = "rgba(124,58,237,0.45)";
      ctx.lineWidth = 1.5;
      for (let x2 = PX0 + 40; x2 < PX1 - 20; x2 += 70) {
        ctx.beginPath();
        ctx.moveTo(x2, TOP + 8);
        ctx.lineTo(x2, BOT - 8);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(x2, BOT - 8);
        ctx.lineTo(x2 - 5, BOT - 18);
        ctx.lineTo(x2 + 5, BOT - 18);
        ctx.closePath();
        ctx.fill();
      }
      vecLabel(ctx, "E", PX0 + 8, (TOP + BOT) / 2, "#7c3aed");
      if (trail.current.length > 1) {
        ctx.strokeStyle = "rgba(220,38,38,0.5)";
        ctx.lineWidth = 2;
        ctx.beginPath();
        trail.current.forEach((pt, i) => i === 0 ? ctx.moveTo(pt.x, pt.y) : ctx.lineTo(pt.x, pt.y));
        ctx.stroke();
      }
      const { x, y } = p.current;
      if (!running2 && trail.current.length === 0) {
        const rad = angle2 * Math.PI / 180;
        arrow(ctx, x, y, Math.cos(rad) * (30 + v0 * 0.18), Math.sin(rad) * (30 + v0 * 0.18), "#16a34a");
        vecLabel(ctx, "v\u2080", x + 6, y - 18, "#16a34a");
      }
      const F = q * E;
      const fl = Math.sign(F) * Math.min(70, 16 + Math.abs(F) * 7);
      if (Math.abs(fl) > 2) {
        arrow(ctx, x, y, 0, fl, "#db2777");
        vecLabel(ctx, "F", x + 10, y + fl + (fl > 0 ? 16 : -4), "#db2777");
      }
      const g = ctx.createRadialGradient(x - 5, y - 5, 2, x, y, 13);
      g.addColorStop(0, "#ffffff");
      g.addColorStop(0.4, q >= 0 ? "#f87171" : "#60a5fa");
      g.addColorStop(1, q >= 0 ? "#b91c1c" : "#1d4ed8");
      ctx.fillStyle = g;
      ctx.beginPath();
      ctx.arc(x, y, 13, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = "#fff";
      ctx.font = "bold 14px sans-serif";
      ctx.textAlign = "center";
      ctx.fillText(q >= 0 ? "+" : "\u2212", x, y + 5);
      ctx.textAlign = "left";
    }, [E, q, v0, angle2, running2]);
    const tick = (0, import_react22.useCallback)((ts) => {
      if (lastRef.current == null)
        lastRef.current = ts;
      const dt = Math.min(0.04, (ts - lastRef.current) / 1e3);
      lastRef.current = ts;
      const ay = q * E * 16;
      v.current.y += ay * dt;
      p.current.x += v.current.x * dt;
      p.current.y += v.current.y * dt;
      trail.current.push({ x: p.current.x, y: p.current.y });
      let stop = false;
      if (p.current.y >= BOT - 13) {
        p.current.y = BOT - 13;
        stop = true;
      }
      if (p.current.y <= TOP + 13) {
        p.current.y = TOP + 13;
        stop = true;
      }
      if (p.current.x >= PX1 || p.current.x <= PX0)
        stop = true;
      force((n) => n + 1);
      draw();
      if (stop) {
        setRunning(false);
        return;
      }
      raf.current = requestAnimationFrame(tick);
    }, [q, E, draw]);
    (0, import_react22.useEffect)(() => {
      if (running2) {
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        lastRef.current = null;
        raf.current = requestAnimationFrame(tick);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, tick, onComplete]);
    (0, import_react22.useEffect)(() => {
      draw();
    }, [draw]);
    const launch = () => {
      const rad = angle2 * Math.PI / 180;
      v.current = { x: Math.cos(rad) * v0, y: Math.sin(rad) * v0 };
      trail.current = [];
      setRunning(true);
    };
    const dragRef = useSimRef("ElectricFieldSimulation.dragRef", false);
    const toLogic = (e) => {
      const r = cv.current.getBoundingClientRect();
      return { x: (e.clientX - r.left) / r.width * W12, y: (e.clientY - r.top) / r.height * H12 };
    };
    const onDown = (e) => {
      if (running2)
        return;
      const m = toLogic(e);
      if (Math.hypot(m.x - p.current.x, m.y - p.current.y) < 22) {
        dragRef.current = true;
        trail.current = [];
        e.target.setPointerCapture?.(e.pointerId);
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
      }
    };
    const onMove = (e) => {
      if (!dragRef.current)
        return;
      const m = toLogic(e);
      p.current = { x: Math.max(PX0 + 14, Math.min(PX1 - 14, m.x)), y: Math.max(TOP + 14, Math.min(BOT - 14, m.y)) };
      v.current = { x: 0, y: 0 };
      force((n) => n + 1);
      draw();
    };
    const onUp = () => {
      dragRef.current = false;
    };
    return /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("canvas", { ref: cv, width: W12, height: H12, onPointerDown: onDown, onPointerMove: onMove, onPointerUp: onUp, onPointerLeave: onUp, className: "w-full rounded-lg border border-gray-100 bg-slate-50 touch-none cursor-grab active:cursor-grabbing" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("div", { className: "grid grid-cols-1 md:grid-cols-2 gap-x-4 gap-y-2 mt-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("span", { className: "w-28 text-gray-600", children: "V\u1EADn t\u1ED1c v\u2080" }),
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("input", { type: "range", min: 0, max: 260, step: 10, value: v0, onChange: (e) => setV0(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("span", { className: "w-12 text-right font-semibold", children: v0 })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("span", { className: "w-28 text-gray-600", children: "H\u01B0\u1EDBng v\u2080" }),
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("input", { type: "range", min: -90, max: 90, step: 5, value: angle2, onChange: (e) => setAngle(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              angle2,
              "\xB0"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("span", { className: "w-28 text-gray-600", children: "C\u01B0\u1EDDng \u0111\u1ED9 E" }),
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("input", { type: "range", min: 0, max: 6, step: 1, value: E, onChange: (e) => setE(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("span", { className: "w-12 text-right font-semibold", children: E })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("span", { className: "w-28 text-gray-600", children: "\u0110i\u1EC7n t\xEDch q" }),
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("input", { type: "range", min: -3, max: 3, step: 1, value: q, onChange: (e) => setQ(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              q > 0 ? "+" : "",
              q
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("div", { className: "flex gap-2 mt-2", children: [
          /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("button", { onClick: launch, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-violet-600 text-white font-bold hover:bg-violet-700", "data-nb-play": "", children: [
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)(Play, { className: "w-4 h-4" }),
            " B\u1EAFn \u0111i\u1EC7n t\xEDch"
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("button", { onClick: reset, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime19.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " \u0110\u1EB7t l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime19.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("h4", { className: "font-bold text-violet-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime19.jsx)("p", { className: "text-[11px] text-gray-400 mt-1", children: "M\u1EB9o: h\u01B0\u1EDBng v\u2080 = \xB190\xB0 (d\u1ECDc theo E) \u2192 qu\u1EF9 \u0111\u1EA1o TH\u1EB2NG; v\u2080 ngang/xi\xEAn \u2192 PARABOL. q=0 \u2192 \u0111i th\u1EB3ng \u0111\u1EC1u." })
        ] })
      ] })
    ] });
  };
  var ElectricFieldSimulation_default = ElectricFieldSimulation;

  // physics/vendor/simulations/ResistanceOhmSimulation.tsx
  var import_react23 = __toESM(require_react());
  var import_jsx_runtime20 = __toESM(require_jsx_runtime());
  var W9 = 690;
  var H9 = 270;
  var PTOP = 26;
  var PBOT = H9 - 44;
  var ResistanceOhmSimulation = ({ title = "\u0110i\u1EC7n tr\u1EDF & \u0111\u1ECBnh lu\u1EADt Ohm", description = "Ba \u0111\u1ED3 th\u1ECB: I\u2013U c\u1EE7a \u0111i\u1EC7n tr\u1EDF kh\xF4ng \u0111\u1ED5i (\u0111\u01B0\u1EDDng th\u1EB3ng), I\u2013U c\u1EE7a \u0111\xE8n s\u1EE3i \u0111\u1ED1t (\u0111\u01B0\u1EDDng cong) v\xE0 R\u2013T c\u1EE7a NTC (gi\u1EA3m).", onComplete }) => {
    const cv = useSimRef("ResistanceOhmSimulation.cv", null);
    const [U, setU] = useSimState("ResistanceOhmSimulation.U", 6), [R, setR] = useSimState("ResistanceOhmSimulation.R", 3), [T2, setT] = useSimState("ResistanceOhmSimulation.T", 60);
    const firstRef = useSimRef("ResistanceOhmSimulation.firstRef", false);
    const touch = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const bulbIU = (u) => 0.12 * Math.pow(Math.max(0, u), 0.55);
    const ntcR = (t) => 10 * Math.exp(3e3 * (1 / (t + 273) - 1 / 298));
    const draw = (0, import_react23.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W9, H9);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W9, H9);
      const panel = (px0, pw2, o) => {
        const x0 = px0 + 36, x1 = px0 + pw2 - 12, y0 = PBOT, yTop = PTOP;
        const X = (x) => x0 + x / o.xmax * (x1 - x0);
        const Y = (y) => y0 - Math.min(y, o.ymax) / o.ymax * (y0 - yTop);
        ctx.strokeStyle = "#eef2f7";
        ctx.lineWidth = 1;
        for (let i = 1; i <= 4; i++) {
          const gy = y0 - i / 4 * (y0 - yTop);
          ctx.beginPath();
          ctx.moveTo(x0, gy);
          ctx.lineTo(x1, gy);
          ctx.stroke();
        }
        for (let i = 1; i <= 4; i++) {
          const gx = x0 + i / 4 * (x1 - x0);
          ctx.beginPath();
          ctx.moveTo(gx, yTop);
          ctx.lineTo(gx, y0);
          ctx.stroke();
        }
        ctx.strokeStyle = "#1e293b";
        ctx.fillStyle = "#1e293b";
        ctx.lineWidth = 1.8;
        ctx.lineJoin = "round";
        ctx.beginPath();
        ctx.moveTo(x0, yTop - 2);
        ctx.lineTo(x0, y0);
        ctx.lineTo(x1, y0);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(x0, yTop - 8);
        ctx.lineTo(x0 - 4, yTop);
        ctx.lineTo(x0 + 4, yTop);
        ctx.closePath();
        ctx.fill();
        ctx.beginPath();
        ctx.moveTo(x1 + 6, y0);
        ctx.lineTo(x1 - 2, y0 - 4);
        ctx.lineTo(x1 - 2, y0 + 4);
        ctx.closePath();
        ctx.fill();
        ctx.font = "bold 11px sans-serif";
        ctx.fillStyle = "#334155";
        ctx.textAlign = "center";
        ctx.fillText(o.title, (x0 + x1) / 2, 14);
        ctx.font = "10px sans-serif";
        ctx.fillStyle = "#64748b";
        ctx.fillText(o.xl, (x0 + x1) / 2, y0 + 22);
        ctx.save();
        ctx.translate(px0 + 10, (yTop + y0) / 2);
        ctx.rotate(-Math.PI / 2);
        ctx.fillText(o.yl, 0, 0);
        ctx.restore();
        ctx.textAlign = "left";
        ctx.strokeStyle = o.color;
        ctx.lineWidth = 2.6;
        ctx.beginPath();
        for (let i = 0; i <= 80; i++) {
          const x = i / 80 * o.xmax;
          const px2 = X(x), py = Y(o.f(x));
          i === 0 ? ctx.moveTo(px2, py) : ctx.lineTo(px2, py);
        }
        ctx.stroke();
        const my = o.f(o.mx);
        ctx.strokeStyle = "rgba(100,116,139,0.5)";
        ctx.lineWidth = 1;
        ctx.setLineDash([3, 3]);
        ctx.beginPath();
        ctx.moveTo(X(o.mx), y0);
        ctx.lineTo(X(o.mx), Y(my));
        ctx.lineTo(x0, Y(my));
        ctx.stroke();
        ctx.setLineDash([]);
        ctx.fillStyle = o.color;
        ctx.beginPath();
        ctx.arc(X(o.mx), Y(my), 4.5, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = "#0f172a";
        ctx.font = "bold 10px sans-serif";
        ctx.textAlign = "center";
        ctx.fillText(`${my.toFixed(my < 1 ? 2 : my < 10 ? 1 : 0)}${o.xunit || ""}`, X(o.mx), Y(my) - 7);
        ctx.textAlign = "left";
      };
      const pw = W9 / 3;
      panel(0, pw, { title: "I\u2013U \u0111i\u1EC7n tr\u1EDF (R kh\xF4ng \u0111\u1ED5i)", xl: "U (V)", yl: "I (A)", xmax: 12, ymax: 6, color: "#2563eb", f: (u) => u / R, mx: U });
      panel(pw, pw, { title: "I\u2013U \u0111\xE8n s\u1EE3i \u0111\u1ED1t", xl: "U (V)", yl: "I (A)", xmax: 12, ymax: 0.5, color: "#ea580c", f: bulbIU, mx: U });
      panel(2 * pw, pw, { title: "R\u2013T c\u1EE7a NTC", xl: "T (\xB0C)", yl: "R (\u03A9)", xmax: 200, ymax: 26, color: "#7c3aed", f: ntcR, mx: T2 });
    }, [U, R, T2]);
    (0, import_react23.useEffect)(() => {
      draw();
    }, [draw]);
    return /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("canvas", { ref: cv, width: W9, height: H9, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("div", { className: "grid grid-cols-1 md:grid-cols-3 gap-x-4 gap-y-2 mt-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("span", { className: "w-20 text-blue-700", children: "U (V)" }),
            /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("input", { type: "range", min: 0, max: 12, step: 0.5, value: U, onChange: (e) => {
              touch();
              setU(Number(e.target.value));
            }, className: "flex-1 accent-blue-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              U,
              " V"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("span", { className: "w-20 text-blue-700", children: "R (\u03A9)" }),
            /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("input", { type: "range", min: 2, max: 6, step: 0.5, value: R, onChange: (e) => {
              touch();
              setR(Number(e.target.value));
            }, className: "flex-1 accent-blue-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              R,
              " \u03A9"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("span", { className: "w-20 text-violet-700", children: "T (\xB0C)" }),
            /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("input", { type: "range", min: 0, max: 200, step: 5, value: T2, onChange: (e) => {
              touch();
              setT(Number(e.target.value));
            }, className: "flex-1 accent-violet-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              T2,
              "\xB0C"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("h4", { className: "font-bold text-blue-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime20.jsxs)("p", { className: "text-[11px] text-gray-400 mt-2", children: [
            "\u0110i\u1EC7n tr\u1EDF kh\xF4ng \u0111\u1ED5i: I t\u1EC9 l\u1EC7 thu\u1EADn U (\u0111\u01B0\u1EDDng th\u1EB3ng qua g\u1ED1c, \u0111\u1ED9 d\u1ED1c = 1/R). ",
            /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("b", { children: "\u0110\xE8n s\u1EE3i \u0111\u1ED1t" }),
            ": \u0111\u1EB7c tuy\u1EBFn I\u2013U l\xE0 ",
            /* @__PURE__ */ (0, import_jsx_runtime20.jsx)("b", { children: "\u0111\u01B0\u1EDDng cong l\xF5m" }),
            " \u2014 khi U t\u0103ng d\xE2y t\xF3c n\xF3ng l\xEAn, R t\u0103ng n\xEAn I t\u0103ng ch\u1EADm d\u1EA7n (U,I \u0111i\u1EC1u khi\u1EC3n b\u1EB1ng thanh U). NTC: R gi\u1EA3m m\u1EA1nh khi nhi\u1EC7t \u0111\u1ED9 T t\u0103ng."
          ] })
        ] })
      ] })
    ] });
  };
  var ResistanceOhmSimulation_default = ResistanceOhmSimulation;

  // physics/vendor/simulations/EMFMeasurementSimulation.tsx
  var import_react24 = __toESM(require_react());
  var import_jsx_runtime21 = __toESM(require_jsx_runtime());
  var W10 = 620;
  var H10 = 300;
  var NOISE = [0.03, -0.05, 0.02, -0.02, 0.04, -0.03, 0.01, -0.04, 0.03];
  var EMFMeasurementSimulation = ({ title = "\u0110o su\u1EA5t \u0111i\u1EC7n \u0111\u1ED9ng & \u0111i\u1EC7n tr\u1EDF trong (\u0111\u1ED3 th\u1ECB I\u2013U)", description = "M\u1EA1ch \u0111o: U = E \u2212 I\xB7r. \u0110\u1ED3 th\u1ECB U theo I l\xE0 \u0111\u01B0\u1EDDng th\u1EB3ng \u2014 giao v\u1EDBi tr\u1EE5c U cho su\u1EA5t \u0111i\u1EC7n \u0111\u1ED9ng E, \u0111\u1ED9 d\u1ED1c cho \u0111i\u1EC7n tr\u1EDF trong r.", onComplete }) => {
    const cv = useSimRef("EMFMeasurementSimulation.cv", null);
    const [E, setE] = useSimState("EMFMeasurementSimulation.E", 4.5), [r, setRr] = useSimState("EMFMeasurementSimulation.r", 1.2);
    const firstRef = useSimRef("EMFMeasurementSimulation.firstRef", false);
    const touch = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const draw = (0, import_react24.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W10, H10);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W10, H10);
      const x0 = 56, y0 = H10 - 44, x1 = W10 - 18, yTop = 22;
      const Imax = E / r, xmax = Math.ceil(Imax * 1.05 * 10) / 10, ymax = Math.ceil(E * 1.15);
      const X = (i) => x0 + i / xmax * (x1 - x0);
      const Y = (u) => y0 - u / ymax * (y0 - yTop);
      ctx.font = "10px sans-serif";
      ctx.strokeStyle = "#eef2f7";
      ctx.lineWidth = 1;
      ctx.fillStyle = "#94a3b8";
      ctx.textAlign = "right";
      for (let u = 1; u <= ymax; u++) {
        ctx.beginPath();
        ctx.moveTo(x0, Y(u));
        ctx.lineTo(x1, Y(u));
        ctx.stroke();
        ctx.fillText(String(u), x0 - 5, Y(u) + 3);
      }
      ctx.textAlign = "center";
      for (let i = 0.5; i <= xmax; i += 0.5) {
        ctx.beginPath();
        ctx.moveTo(X(i), yTop);
        ctx.lineTo(X(i), y0);
        ctx.stroke();
        ctx.fillText(i.toFixed(1), X(i), y0 + 14);
      }
      ctx.textAlign = "left";
      ctx.strokeStyle = "#1e293b";
      ctx.fillStyle = "#1e293b";
      ctx.lineWidth = 2;
      ctx.lineJoin = "round";
      ctx.beginPath();
      ctx.moveTo(x0, yTop - 2);
      ctx.lineTo(x0, y0);
      ctx.lineTo(x1, y0);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x0, yTop - 8);
      ctx.lineTo(x0 - 4, yTop);
      ctx.lineTo(x0 + 4, yTop);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(x1 + 6, y0);
      ctx.lineTo(x1 - 2, y0 - 4);
      ctx.lineTo(x1 - 2, y0 + 4);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("U (V)", x0 + 4, 14);
      ctx.fillText("I (A)", x1 - 30, y0 + 24);
      ctx.strokeStyle = "#2563eb";
      ctx.lineWidth = 2.6;
      ctx.beginPath();
      ctx.moveTo(X(0), Y(E));
      ctx.lineTo(X(Imax), Y(0));
      ctx.stroke();
      ctx.fillStyle = "#dc2626";
      for (let k = 0; k < 9; k++) {
        const I = k / 9 * Imax * 0.95;
        const U = E - I * r + NOISE[k] * E;
        ctx.beginPath();
        ctx.arc(X(I), Y(U), 4, 0, Math.PI * 2);
        ctx.fill();
      }
      ctx.fillStyle = "#16a34a";
      ctx.beginPath();
      ctx.arc(X(0), Y(E), 5, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = "rgba(22,163,74,0.5)";
      ctx.setLineDash([4, 3]);
      ctx.beginPath();
      ctx.moveTo(x0, Y(E));
      ctx.lineTo(X(Imax * 0.3), Y(E));
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#0f172a";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText(`E = ${E.toFixed(1)} V (giao tr\u1EE5c U)`, X(Imax * 0.32), Y(E) - 6);
      ctx.fillStyle = "#334155";
      ctx.fillText(`U = E \u2212 I\xB7r   \u2192   r = ${r.toFixed(1)} \u03A9 (\u0111\u1ED9 d\u1ED1c)`, x0 + 8, Y(E * 0.45));
    }, [E, r]);
    (0, import_react24.useEffect)(() => {
      draw();
    }, [draw]);
    return /* @__PURE__ */ (0, import_jsx_runtime21.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("canvas", { ref: cv, width: W10, height: H10, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime21.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime21.jsxs)("div", { className: "grid grid-cols-1 md:grid-cols-2 gap-x-4 gap-y-2 mt-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime21.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("span", { className: "w-36 text-gray-600", children: "Su\u1EA5t \u0111i\u1EC7n \u0111\u1ED9ng E" }),
            /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("input", { type: "range", min: 1.5, max: 9, step: 0.5, value: E, onChange: (e) => {
              touch();
              setE(Number(e.target.value));
            }, className: "flex-1 accent-amber-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime21.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              E,
              " V"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime21.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("span", { className: "w-36 text-gray-600", children: "\u0110i\u1EC7n tr\u1EDF trong r" }),
            /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("input", { type: "range", min: 0.2, max: 3, step: 0.1, value: r, onChange: (e) => {
              touch();
              setRr(Number(e.target.value));
            }, className: "flex-1 accent-amber-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime21.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              r,
              " \u03A9"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime21.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("h4", { className: "font-bold text-amber-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime21.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "\u0110o U \u1EE9ng v\u1EDBi c\xE1c gi\xE1 tr\u1ECB I kh\xE1c nhau (\u0111i\u1EC3m \u0111\u1ECF) \u2192 v\u1EBD \u0111\u01B0\u1EDDng th\u1EB3ng U = E \u2212 I\xB7r. Tung \u0111\u1ED9 g\u1ED1c (I = 0) b\u1EB1ng su\u1EA5t \u0111i\u1EC7n \u0111\u1ED9ng E; \u0111\u1ED9 d\u1ED1c \u0111\u01B0\u1EDDng th\u1EB3ng b\u1EB1ng \u2212r \u2192 suy ra \u0111i\u1EC7n tr\u1EDF trong." })
        ] })
      ] })
    ] });
  };
  var EMFMeasurementSimulation_default = EMFMeasurementSimulation;

  // physics/vendor/simulations/GasLawSimulation.tsx
  var import_react25 = __toESM(require_react());
  var import_jsx_runtime22 = __toESM(require_jsx_runtime());
  var GasLawSimulation = ({ title = "\u0110\u1ECBnh lu\u1EADt ch\u1EA5t kh\xED \u2014 pV/T = h\u1EB1ng s\u1ED1", description = "Kh\xED trong xilanh c\xF3 pitt\xF4ng. T\u0103ng nhi\u1EC7t \u0111\u1ED9 T (gi\u1EEF p) \u2192 th\u1EC3 t\xEDch V t\u0103ng (\u0111\u1ECBnh lu\u1EADt Charles). \u0110\u1EA1i l\u01B0\u1EE3ng pV/T lu\xF4n kh\xF4ng \u0111\u1ED5i.", onComplete }) => {
    const cv = useSimRef("GasLawSimulation.cv", null);
    const raf = useSimRef("GasLawSimulation.raf", null);
    const ps = useSimRef("GasLawSimulation.ps", []);
    const [running2, setRunning] = useSimState("GasLawSimulation.running", true);
    const [T2, setT] = useSimState("GasLawSimulation.T", 300);
    const [p, setP] = useSimState("GasLawSimulation.p", 2);
    const firstRef = useSimRef("GasLawSimulation.firstRef", false);
    const W12 = 300, H12 = 320;
    const cylX = 70, cylW = 160, cylBottom = H12 - 20, cylTopMax = 30;
    const Vnorm = T2 / 300 / p;
    const gasH = Math.max(40, Math.min(cylBottom - cylTopMax, Vnorm * 150));
    const pistonY = cylBottom - gasH;
    const build = (0, import_react25.useCallback)(() => {
      ps.current = Array.from({ length: 40 }, () => {
        const a = Math.random() * Math.PI * 2, sp = 0.8 + T2 / 300 * 1.2;
        return { x: cylX + 10 + Math.random() * (cylW - 20), y: pistonY + 10 + Math.random() * (gasH - 20), vx: Math.cos(a) * sp, vy: Math.sin(a) * sp };
      });
    }, []);
    (0, import_react25.useEffect)(() => {
      build();
    }, []);
    (0, import_react25.useEffect)(() => {
      const sp = 0.8 + T2 / 300 * 1.2;
      ps.current.forEach((q) => {
        const m = Math.hypot(q.vx, q.vy) || 1;
        q.vx = q.vx / m * sp;
        q.vy = q.vy / m * sp;
      });
    }, [T2]);
    const draw = (0, import_react25.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      ctx.strokeStyle = "#94a3b8";
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.moveTo(cylX, cylTopMax - 6);
      ctx.lineTo(cylX, cylBottom);
      ctx.lineTo(cylX + cylW, cylBottom);
      ctx.lineTo(cylX + cylW, cylTopMax - 6);
      ctx.stroke();
      ctx.fillStyle = "rgba(59,130,246,0.06)";
      ctx.fillRect(cylX, pistonY, cylW, gasH);
      for (const q of ps.current) {
        q.x += q.vx;
        q.y += q.vy;
        if (q.x < cylX + 5) {
          q.x = cylX + 5;
          q.vx = Math.abs(q.vx);
        }
        if (q.x > cylX + cylW - 5) {
          q.x = cylX + cylW - 5;
          q.vx = -Math.abs(q.vx);
        }
        if (q.y < pistonY + 5) {
          q.y = pistonY + 5;
          q.vy = Math.abs(q.vy);
        }
        if (q.y > cylBottom - 5) {
          q.y = cylBottom - 5;
          q.vy = -Math.abs(q.vy);
        }
        ctx.beginPath();
        ctx.arc(q.x, q.y, 3.5, 0, Math.PI * 2);
        ctx.fillStyle = "#3b82f6";
        ctx.fill();
      }
      ctx.fillStyle = "#64748b";
      ctx.fillRect(cylX - 4, pistonY - 12, cylW + 8, 12);
      ctx.fillStyle = "#475569";
      ctx.font = "bold 12px sans-serif";
      for (let i = 0; i < p; i++)
        ctx.fillRect(cylX + cylW / 2 - 18 + i % 3 * 14, pistonY - 12 - 12 * (Math.floor(i / 3) + 1), 12, 10);
      raf.current = requestAnimationFrame(draw);
    }, [gasH, pistonY, p]);
    (0, import_react25.useEffect)(() => {
      if (running2) {
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        raf.current = requestAnimationFrame(draw);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, draw, onComplete]);
    (0, import_react25.useEffect)(() => {
      if (!running2)
        draw();
    }, [draw, running2]);
    const pVT = (p * Vnorm / (T2 / 300)).toFixed(2);
    return /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("canvas", { ref: cv, width: W12, height: H12, className: "rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("div", { className: "flex-1", children: [
          /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("div", { className: "grid grid-cols-3 gap-2 text-center mb-3", children: [
            /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("div", { className: "p-2 rounded-lg bg-rose-50", children: [
              /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("div", { className: "font-black text-rose-700", children: p }),
              /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("div", { className: "text-[11px] text-gray-500", children: "\xC1p su\u1EA5t p" })
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("div", { className: "p-2 rounded-lg bg-cyan-50", children: [
              /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("div", { className: "font-black text-cyan-700", children: Vnorm.toFixed(2) }),
              /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("div", { className: "text-[11px] text-gray-500", children: "Th\u1EC3 t\xEDch V" })
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("div", { className: "p-2 rounded-lg bg-amber-50", children: [
              /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("div", { className: "font-black text-amber-700", children: [
                T2,
                "K"
              ] }),
              /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("div", { className: "text-[11px] text-gray-500", children: "Nhi\u1EC7t \u0111\u1ED9 T" })
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("div", { className: "text-center p-2 rounded-xl bg-emerald-50 mb-3", children: [
            /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("span", { className: "font-black text-emerald-700", children: [
              "pV/T = ",
              pVT
            ] }),
            " ",
            /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("span", { className: "text-xs text-gray-500", children: "(h\u1EB1ng s\u1ED1)" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("label", { className: "flex items-center gap-2 text-sm mb-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("span", { className: "w-24 text-gray-600", children: "Nhi\u1EC7t \u0111\u1ED9 T" }),
            /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("input", { type: "range", min: 150, max: 500, step: 10, value: T2, onChange: (e) => setT(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              T2,
              "K"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("label", { className: "flex items-center gap-2 text-sm mb-3", children: [
            /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("span", { className: "w-24 text-gray-600", children: "\xC1p su\u1EA5t p" }),
            /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("input", { type: "range", min: 1, max: 6, step: 1, value: p, onChange: (e) => setP(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("span", { className: "w-12 text-right font-semibold", children: p })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("button", { onClick: () => setRunning((x) => !x), className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-cyan-600 text-white font-bold hover:bg-cyan-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)(import_jsx_runtime22.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime22.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)(import_jsx_runtime22.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime22.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime22.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("h4", { className: "font-bold text-cyan-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime22.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var GasLawSimulation_default = GasLawSimulation;

  // physics/vendor/simulations/GasParticleSimulation.tsx
  var import_react26 = __toESM(require_react());
  var import_jsx_runtime23 = __toESM(require_jsx_runtime());
  var GasParticleSimulation = ({ title = "M\xF4 h\xECnh \u0111\u1ED9ng h\u1ECDc ph\xE2n t\u1EED ch\u1EA5t kh\xED", description = "Ph\xE2n t\u1EED kh\xED chuy\u1EC3n \u0111\u1ED9ng h\u1ED7n lo\u1EA1n v\xE0 va ch\u1EA1m v\xE0o th\xE0nh b\xECnh t\u1EA1o \xE1p su\u1EA5t. T\u0103ng nhi\u1EC7t \u0111\u1ED9 \u2192 \u0111\u1ED9ng n\u0103ng & t\u1ED1c \u0111\u1ED9 t\u0103ng.", onComplete }) => {
    const cv = useSimRef("GasParticleSimulation.cv", null);
    const raf = useSimRef("GasParticleSimulation.raf", null);
    const ps = useSimRef("GasParticleSimulation.ps", []);
    const hitsRef = useSimRef("GasParticleSimulation.hitsRef", 0);
    const [running2, setRunning] = useSimState("GasParticleSimulation.running", true);
    const [temp, setTemp] = useSimState("GasParticleSimulation.temp", 5);
    const [count, setCount] = useSimState("GasParticleSimulation.count", 60);
    const [pressure, setPressure] = useSimState("GasParticleSimulation.pressure", 0);
    const firstRef = useSimRef("GasParticleSimulation.firstRef", false);
    const W12 = 460, H12 = 300;
    const speedForTemp = (t) => 0.6 + t * 0.5;
    const build = (0, import_react26.useCallback)((n, t) => {
      const sp = speedForTemp(t);
      ps.current = Array.from({ length: n }, () => {
        const a = Math.random() * Math.PI * 2;
        return { x: 20 + Math.random() * (W12 - 40), y: 20 + Math.random() * (H12 - 40), vx: Math.cos(a) * sp, vy: Math.sin(a) * sp };
      });
    }, []);
    const reset = (0, import_react26.useCallback)(() => {
      build(count, temp);
      hitsRef.current = 0;
      setPressure(0);
    }, [count, temp, build]);
    (0, import_react26.useEffect)(() => {
      build(count, temp);
    }, [count]);
    (0, import_react26.useEffect)(() => {
      const sp = speedForTemp(temp);
      ps.current.forEach((p) => {
        const m = Math.hypot(p.vx, p.vy) || 1;
        p.vx = p.vx / m * sp;
        p.vy = p.vy / m * sp;
      });
    }, [temp]);
    const tick = (0, import_react26.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      ctx.strokeStyle = "#94a3b8";
      ctx.lineWidth = 2;
      ctx.strokeRect(6, 6, W12 - 12, H12 - 12);
      let hits = 0;
      for (const p of ps.current) {
        p.x += p.vx;
        p.y += p.vy;
        if (p.x < 12) {
          p.x = 12;
          p.vx = Math.abs(p.vx);
          hits++;
        }
        if (p.x > W12 - 12) {
          p.x = W12 - 12;
          p.vx = -Math.abs(p.vx);
          hits++;
        }
        if (p.y < 12) {
          p.y = 12;
          p.vy = Math.abs(p.vy);
          hits++;
        }
        if (p.y > H12 - 12) {
          p.y = H12 - 12;
          p.vy = -Math.abs(p.vy);
          hits++;
        }
        ctx.beginPath();
        ctx.arc(p.x, p.y, 4, 0, Math.PI * 2);
        ctx.fillStyle = "#3b82f6";
        ctx.fill();
      }
      hitsRef.current = hitsRef.current * 0.9 + hits * 0.1;
      setPressure(Math.round(hitsRef.current * 20));
      raf.current = requestAnimationFrame(tick);
    }, []);
    (0, import_react26.useEffect)(() => {
      if (running2) {
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        raf.current = requestAnimationFrame(tick);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, tick, onComplete]);
    return /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("canvas", { ref: cv, width: W12, height: H12, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("div", { className: "grid grid-cols-2 gap-2 my-3 text-center", children: [
          /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("div", { className: "p-2 rounded-lg bg-rose-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("div", { className: "text-lg font-black text-rose-700", children: pressure }),
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("div", { className: "text-[11px] text-gray-500", children: "\xC1p su\u1EA5t (t\u01B0\u01A1ng \u0111\u1ED1i)" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("div", { className: "p-2 rounded-lg bg-amber-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("div", { className: "text-lg font-black text-amber-700", children: [
              (temp * 30).toFixed(0),
              " K"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("div", { className: "text-[11px] text-gray-500", children: "\u0110\u1ED9ng n\u0103ng TB ~ T" })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("div", { className: "space-y-2", children: [
          /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("span", { className: "w-28 text-gray-600", children: "Nhi\u1EC7t \u0111\u1ED9" }),
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("input", { type: "range", min: 1, max: 10, value: temp, onChange: (e) => setTemp(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("span", { className: "w-10 text-right font-semibold", children: temp })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("span", { className: "w-28 text-gray-600", children: "S\u1ED1 ph\xE2n t\u1EED" }),
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("input", { type: "range", min: 20, max: 150, step: 10, value: count, onChange: (e) => setCount(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("span", { className: "w-10 text-right font-semibold", children: count })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("button", { onClick: () => setRunning((r) => !r), className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)(import_jsx_runtime23.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)(import_jsx_runtime23.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("button", { onClick: reset, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime23.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " \u0110\u1EB7t l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime23.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("h4", { className: "font-bold text-blue-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime23.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var GasParticleSimulation_default = GasParticleSimulation;

  // physics/vendor/simulations/HeatingCurveSimulation.tsx
  var import_react27 = __toESM(require_react());
  var import_jsx_runtime24 = __toESM(require_jsx_runtime());
  var SEG = [0, 0.15, 0.35, 0.55, 0.85, 1];
  var T0 = -20;
  var Tm = 0;
  var Tb = 100;
  var Tend = 140;
  function tempAt(q) {
    if (q <= SEG[1])
      return T0 + (Tm - T0) * (q - SEG[0]) / (SEG[1] - SEG[0]);
    if (q <= SEG[2])
      return Tm;
    if (q <= SEG[3])
      return Tm + (Tb - Tm) * (q - SEG[2]) / (SEG[3] - SEG[2]);
    if (q <= SEG[4])
      return Tb;
    return Tb + (Tend - Tb) * (q - SEG[4]) / (SEG[5] - SEG[4]);
  }
  function stateAt(q) {
    if (q < SEG[1])
      return { label: "R\u1EAFn (\u0111ang n\xF3ng l\xEAn)", cls: "bg-blue-600" };
    if (q < SEG[2])
      return { label: "\u0110ang n\xF3ng ch\u1EA3y", cls: "bg-cyan-600" };
    if (q < SEG[3])
      return { label: "L\u1ECFng (\u0111ang n\xF3ng l\xEAn)", cls: "bg-teal-600" };
    if (q < SEG[4])
      return { label: "\u0110ang s\xF4i / ho\xE1 h\u01A1i", cls: "bg-orange-500" };
    return { label: "H\u01A1i", cls: "bg-red-500" };
  }
  var HeatingCurveSimulation = ({ title = "\u0110\u01B0\u1EDDng cong Nhi\u1EC7t \u0111\u1ED9 \u2013 Nhi\u1EC7t l\u01B0\u1EE3ng", description = "Cung c\u1EA5p nhi\u1EC7t l\u01B0\u1EE3ng Q cho ch\u1EA5t: nhi\u1EC7t \u0111\u1ED9 t\u0103ng r\u1ED3i \u0111i ngang khi chuy\u1EC3n th\u1EC3 (n\xF3ng ch\u1EA3y, ho\xE1 h\u01A1i).", onComplete }) => {
    const cv = useSimRef("HeatingCurveSimulation.cv", null);
    const [q, setQ] = useSimState("HeatingCurveSimulation.q", 0);
    const firstRef = useSimRef("HeatingCurveSimulation.firstRef", false);
    const W12 = 680, H12 = 320;
    const draw = (0, import_react27.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      const x0 = 58, y0 = H12 - 36, gw = W12 - x0 - 22, gh = H12 - 60;
      const X = (qq) => x0 + qq * gw;
      const Y = (t) => y0 - (t - T0) / (Tend - T0) * gh;
      ctx.strokeStyle = "#eef2f7";
      ctx.lineWidth = 1;
      for (let i = 0; i <= 10; i++) {
        const gx = x0 + i / 10 * gw;
        ctx.beginPath();
        ctx.moveTo(gx, 20);
        ctx.lineTo(gx, y0);
        ctx.stroke();
      }
      ctx.strokeStyle = "#1e293b";
      ctx.fillStyle = "#1e293b";
      ctx.lineWidth = 2.5;
      ctx.lineJoin = "round";
      ctx.beginPath();
      ctx.moveTo(x0, 14);
      ctx.lineTo(x0, y0);
      ctx.lineTo(x0 + gw, y0);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(x0, 7);
      ctx.lineTo(x0 - 5, 17);
      ctx.lineTo(x0 + 5, 17);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(x0 + gw + 7, y0);
      ctx.lineTo(x0 + gw - 4, y0 - 5);
      ctx.lineTo(x0 + gw - 4, y0 + 5);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 13px sans-serif";
      ctx.textAlign = "right";
      ctx.fillText("Th\u1EDDi gian (s)", x0 + gw + 2, y0 + 18);
      ctx.textAlign = "left";
      ctx.save();
      ctx.translate(14, (14 + y0) / 2);
      ctx.rotate(-Math.PI / 2);
      ctx.textAlign = "center";
      ctx.fillText("Nhi\u1EC7t \u0111\u1ED9 (\xB0C)", 0, 0);
      ctx.restore();
      ctx.textAlign = "left";
      const subLbl = (sub, y) => {
        ctx.fillStyle = "#334155";
        ctx.font = "bold 13px sans-serif";
        ctx.fillText("T", x0 - 28, y + 4);
        ctx.font = "9px sans-serif";
        ctx.fillText(sub, x0 - 28 + ctx.measureText("T").width + 6, y + 7);
      };
      ctx.strokeStyle = "rgba(148,163,184,0.6)";
      ctx.setLineDash([4, 3]);
      ctx.lineWidth = 1;
      [{ t: Tm, s: "nc" }, { t: Tb, s: "s" }].forEach(({ t, s }) => {
        ctx.beginPath();
        ctx.moveTo(x0, Y(t));
        ctx.lineTo(x0 + gw, Y(t));
        ctx.stroke();
        subLbl(s, Y(t));
      });
      ctx.setLineDash([]);
      ctx.strokeStyle = "#2563eb";
      ctx.lineWidth = 3.5;
      ctx.beginPath();
      const iMaxH = Math.max(0, Math.ceil(q * 200));
      for (let i = 0; i <= iMaxH; i++) {
        const qq = Math.min(q, i / 200);
        const px3 = X(qq), py2 = Y(tempAt(qq));
        i === 0 ? ctx.moveTo(px3, py2) : ctx.lineTo(px3, py2);
      }
      ctx.stroke();
      const px2 = X(q), py = Y(tempAt(q));
      ctx.strokeStyle = "rgba(239,68,68,0.5)";
      ctx.lineWidth = 1.5;
      ctx.setLineDash([3, 3]);
      ctx.beginPath();
      ctx.moveTo(px2, y0);
      ctx.lineTo(px2, py);
      ctx.moveTo(x0, py);
      ctx.lineTo(px2, py);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#ef4444";
      ctx.beginPath();
      ctx.arc(px2, py, 6, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = "#fff";
      ctx.lineWidth = 2;
      ctx.stroke();
    }, [q]);
    (0, import_react27.useEffect)(() => {
      draw();
    }, [draw]);
    const st = stateAt(q);
    return /* @__PURE__ */ (0, import_jsx_runtime24.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime24.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime24.jsx)("canvas", { ref: cv, width: W12, height: H12, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime24.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime24.jsxs)("div", { className: "flex items-center justify-center gap-3 my-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime24.jsxs)("span", { className: "text-sm text-gray-500", children: [
            "Nhi\u1EC7t \u0111\u1ED9: ",
            /* @__PURE__ */ (0, import_jsx_runtime24.jsxs)("b", { className: "text-blue-700", children: [
              tempAt(q).toFixed(0),
              "\xB0C"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime24.jsx)("span", { className: `px-3 py-1 rounded-full font-bold text-white ${st.cls}`, children: st.label })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime24.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime24.jsx)("span", { className: "w-32 text-gray-600 font-semibold", children: "Th\u1EDDi gian (\u0111un)" }),
          /* @__PURE__ */ (0, import_jsx_runtime24.jsx)("input", { type: "range", min: 0, max: 1, step: 0.01, value: q, onChange: (e) => {
            if (!firstRef.current) {
              firstRef.current = true;
              onComplete?.();
            }
            setQ(Number(e.target.value));
          }, className: "flex-1 accent-red-500" }),
          /* @__PURE__ */ (0, import_jsx_runtime24.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
            Math.round(q * 100),
            "%"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime24.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime24.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime24.jsx)("h4", { className: "font-bold text-blue-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime24.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var HeatingCurveSimulation_default = HeatingCurveSimulation;

  // physics/vendor/simulations/SpecificHeatSimulation.tsx
  var import_react28 = __toESM(require_react());
  var import_jsx_runtime25 = __toESM(require_jsx_runtime());
  var SUBSTANCES = [
    { name: "N\u01B0\u1EDBc", c: 4200 },
    { name: "Nh\xF4m", c: 880 },
    { name: "S\u1EAFt", c: 460 },
    { name: "\u0110\u1ED3ng", c: 380 }
  ];
  var T02 = 20;
  var TMAX = 100;
  var M_MAX = 5;
  var HEAT_CONST = 42e3;
  var SpecificHeatSimulation = ({ title = "Nhi\u1EC7t dung ri\xEAng \u2014 Q = m\xB7c\xB7\u0394T", description = "\u0110un v\u1EDBi c\xF4ng su\u1EA5t kh\xF4ng \u0111\u1ED5i: nhi\u1EC7t \u0111\u1ED9 t\u0103ng theo th\u1EDDi gian. C\xE0ng nhi\u1EC1u n\u01B0\u1EDBc ho\u1EB7c ch\u1EA5t c\xF3 c l\u1EDBn \u2192 n\xF3ng c\xE0ng ch\u1EADm (\u0111\u1ED9 d\u1ED1c \u0111\u1ED3 th\u1ECB nh\u1ECF).", onComplete }) => {
    const cv = useSimRef("SpecificHeatSimulation.cv", null);
    const raf = useSimRef("SpecificHeatSimulation.raf", null);
    const lastRef = useSimRef("SpecificHeatSimulation.lastRef", null);
    const tRef = useSimRef("SpecificHeatSimulation.tRef", 0);
    const TcurRef = useSimRef("SpecificHeatSimulation.TcurRef", T02);
    const histRef = useSimRef("SpecificHeatSimulation.histRef", [{ t: 0, T: T02 }]);
    const [running2, setRunning] = useSimState("SpecificHeatSimulation.running", false);
    const [m, setM] = useSimState("SpecificHeatSimulation.m", 1);
    const [si, setSi] = useSimState("SpecificHeatSimulation.si", 0);
    const [Tdisp, setTdisp] = useSimState("SpecificHeatSimulation.Tdisp", T02);
    const firstRef = useSimRef("SpecificHeatSimulation.firstRef", false);
    const c = SUBSTANCES[si].c;
    const W12 = 620, H12 = 300;
    const reset = (0, import_react28.useCallback)(() => {
      setRunning(false);
      tRef.current = 0;
      TcurRef.current = T02;
      histRef.current = [{ t: 0, T: T02 }];
      setTdisp(T02);
    }, []);
    (0, import_react28.useEffect)(() => {
      reset();
    }, [m, si, reset]);
    const draw = (0, import_react28.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      const T2 = TcurRef.current;
      const heat = (T2 - T02) / (TMAX - T02);
      const bx = 40, bw = 90, bTop = 40, bBottom = H12 - 36, innerH = bBottom - bTop;
      const waterH = m / M_MAX * innerH;
      const waterTop = bBottom - waterH;
      const r = Math.round(60 + heat * 195), g = Math.round(130 - heat * 90), b = Math.round(246 - heat * 200);
      const grd = ctx.createLinearGradient(0, waterTop, 0, bBottom);
      grd.addColorStop(0, `rgb(${r},${g + 40},${b})`);
      grd.addColorStop(1, `rgb(${r},${g},${b})`);
      ctx.fillStyle = grd;
      ctx.fillRect(bx, waterTop, bw, waterH);
      ctx.strokeStyle = "#475569";
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.moveTo(bx, bTop);
      ctx.lineTo(bx, bBottom);
      ctx.lineTo(bx + bw, bBottom);
      ctx.lineTo(bx + bw, bTop);
      ctx.stroke();
      ctx.strokeStyle = "rgba(148,163,184,0.7)";
      ctx.lineWidth = 1;
      ctx.setLineDash([4, 3]);
      ctx.beginPath();
      ctx.moveTo(bx, waterTop);
      ctx.lineTo(bx + bw, waterTop);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#334155";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText(`${m} kg`, bx + bw / 2 - 14, waterTop + 16);
      ctx.font = "20px sans-serif";
      for (let i = 0; i < 3; i++)
        ctx.fillText("\u{1F525}", bx + 14 + i * 22, bBottom + 24);
      const gx0 = 200, gy0 = H12 - 36, gw = W12 - gx0 - 16, gh = H12 - 60;
      const TT = 24;
      ctx.strokeStyle = "#eef2f7";
      ctx.lineWidth = 1;
      for (let i = 0; i <= 8; i++) {
        const gx = gx0 + i / 8 * gw;
        ctx.beginPath();
        ctx.moveTo(gx, 16);
        ctx.lineTo(gx, gy0);
        ctx.stroke();
      }
      ctx.strokeStyle = "#1e293b";
      ctx.fillStyle = "#1e293b";
      ctx.lineWidth = 2.5;
      ctx.lineJoin = "round";
      ctx.beginPath();
      ctx.moveTo(gx0, 14);
      ctx.lineTo(gx0, gy0);
      ctx.lineTo(gx0 + gw, gy0);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(gx0, 7);
      ctx.lineTo(gx0 - 5, 17);
      ctx.lineTo(gx0 + 5, 17);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(gx0 + gw + 7, gy0);
      ctx.lineTo(gx0 + gw - 4, gy0 - 5);
      ctx.lineTo(gx0 + gw - 4, gy0 + 5);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 13px sans-serif";
      ctx.textAlign = "right";
      ctx.fillText("Th\u1EDDi gian (s)", gx0 + gw + 2, gy0 + 18);
      ctx.textAlign = "left";
      ctx.save();
      ctx.translate(gx0 - 16, (14 + gy0) / 2);
      ctx.rotate(-Math.PI / 2);
      ctx.textAlign = "center";
      ctx.fillText("Nhi\u1EC7t \u0111\u1ED9 (\xB0C)", 0, 0);
      ctx.restore();
      ctx.textAlign = "left";
      const X = (t) => gx0 + Math.min(t / TT, 1) * gw;
      const Y = (temp) => gy0 - (temp - T02) / (TMAX - T02) * gh;
      ctx.strokeStyle = "#dc2626";
      ctx.lineWidth = 3;
      ctx.beginPath();
      histRef.current.forEach((p, i) => {
        i === 0 ? ctx.moveTo(X(p.t), Y(p.T)) : ctx.lineTo(X(p.t), Y(p.T));
      });
      ctx.stroke();
      const last = histRef.current[histRef.current.length - 1];
      ctx.fillStyle = "#dc2626";
      ctx.beginPath();
      ctx.arc(X(last.t), Y(last.T), 5, 0, Math.PI * 2);
      ctx.fill();
    }, [m, si]);
    const tick = (0, import_react28.useCallback)((ts) => {
      if (lastRef.current == null)
        lastRef.current = ts;
      const dt = Math.min(0.05, (ts - lastRef.current) / 1e3);
      lastRef.current = ts;
      const rate = HEAT_CONST / (m * c);
      let T2 = TcurRef.current + rate * dt;
      if (T2 >= TMAX) {
        T2 = TMAX;
      }
      TcurRef.current = T2;
      tRef.current += dt;
      const h = histRef.current;
      h.push({ t: tRef.current, T: T2 });
      if (h.length > 1500)
        h.shift();
      setTdisp(T2);
      draw();
      if (T2 >= TMAX || tRef.current >= 24) {
        setRunning(false);
        return;
      }
      raf.current = requestAnimationFrame(tick);
    }, [m, c, draw]);
    (0, import_react28.useEffect)(() => {
      if (running2) {
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        lastRef.current = null;
        raf.current = requestAnimationFrame(tick);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, tick, onComplete]);
    (0, import_react28.useEffect)(() => {
      draw();
    }, [draw]);
    const dT = Math.round(Tdisp - T02);
    const Q = m * c * dT;
    return /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("canvas", { ref: cv, width: W12, height: H12, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "grid grid-cols-2 md:grid-cols-4 gap-2 my-3 text-center", children: [
          /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "p-2 rounded-lg bg-rose-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "font-black text-rose-700", children: [
              Math.round(Tdisp),
              "\xB0C"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("div", { className: "text-[11px] text-gray-500", children: "Nhi\u1EC7t \u0111\u1ED9 T" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "p-2 rounded-lg bg-blue-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "font-black text-blue-700", children: [
              dT,
              "\xB0C"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("div", { className: "text-[11px] text-gray-500", children: "\u0110\u1ED9 t\u0103ng \u0394T" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "p-2 rounded-lg bg-amber-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "font-black text-amber-700", children: [
              (Q / 1e3).toFixed(1),
              " kJ"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("div", { className: "text-[11px] text-gray-500", children: "Q=m\xB7c\xB7\u0394T" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "p-2 rounded-lg bg-emerald-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("div", { className: "font-black text-emerald-700", children: (HEAT_CONST / (m * c)).toFixed(1) }),
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("div", { className: "text-[11px] text-gray-500", children: "\u0110\u1ED9 d\u1ED1c \xB0C/s" })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "grid grid-cols-1 md:grid-cols-2 gap-3 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("span", { className: "w-32 text-gray-600", children: "L\u01B0\u1EE3ng n\u01B0\u1EDBc (c\u1ED9t)" }),
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("input", { type: "range", min: 0.5, max: 5, step: 0.5, value: m, onChange: (e) => setM(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
              m,
              " kg"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("label", { className: "flex items-center gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("span", { className: "w-16 text-gray-600", children: "Ch\u1EA5t" }),
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("select", { value: si, onChange: (e) => setSi(Number(e.target.value)), className: "flex-1 px-2 py-1.5 rounded-lg border border-gray-200 text-sm", children: SUBSTANCES.map((s, i) => /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("option", { value: i, children: [
              s.name,
              " (c=",
              s.c,
              ")"
            ] }, i)) })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("button", { onClick: () => {
            if (TcurRef.current >= TMAX)
              reset();
            setRunning((r) => !r);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-amber-500 text-white font-bold hover:bg-amber-600", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)(import_jsx_runtime25.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)(import_jsx_runtime25.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)(Play, { className: "w-4 h-4" }),
            " \u0110un"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("button", { onClick: reset, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime25.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " \u0110\u1EB7t l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime25.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("h4", { className: "font-bold text-amber-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime25.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var SpecificHeatSimulation_default = SpecificHeatSimulation;

  // physics/vendor/simulations/PhaseChangeSimulation.tsx
  var import_react29 = __toESM(require_react());
  var import_jsx_runtime26 = __toESM(require_jsx_runtime());
  function drawSphere(ctx, x, y, r, hi, base, edge) {
    const g = ctx.createRadialGradient(x - r * 0.35, y - r * 0.35, r * 0.12, x, y, r);
    g.addColorStop(0, hi);
    g.addColorStop(0.45, base);
    g.addColorStop(1, edge);
    ctx.fillStyle = g;
    ctx.beginPath();
    ctx.arc(x, y, r, 0, Math.PI * 2);
    ctx.fill();
  }
  var PhaseChangeSimulation = ({ title = "S\u1EF1 chuy\u1EC3n th\u1EC3 c\u1EE7a ch\u1EA5t", description = "T\u0103ng nhi\u1EC7t \u0111\u1ED9 \u0111\u1EC3 quan s\xE1t chuy\u1EC3n t\u1EEB th\u1EC3 r\u1EAFn (m\u1EA1ng tinh th\u1EC3) \u2192 l\u1ECFng \u2192 kh\xED.", onComplete }) => {
    const cv = useSimRef("PhaseChangeSimulation.cv", null);
    const raf = useSimRef("PhaseChangeSimulation.raf", null);
    const ps = useSimRef("PhaseChangeSimulation.ps", []);
    const [running2, setRunning] = useSimState("PhaseChangeSimulation.running", true);
    const [temp, setTemp] = useSimState("PhaseChangeSimulation.temp", 2);
    const firstRef = useSimRef("PhaseChangeSimulation.firstRef", false);
    const W12 = 460, H12 = 280;
    const phase = temp < 4 ? "r\u1EAFn" : temp < 7 ? "l\u1ECFng" : "kh\xED";
    const build = (0, import_react29.useCallback)(() => {
      const cols = 10, rows = 6, gap = 34, ox = 80, oy = 60;
      const arr = [];
      for (let r = 0; r < rows; r++)
        for (let c = 0; c < cols; c++) {
          const hx = ox + c * gap, hy = oy + r * gap;
          arr.push({ x: hx, y: hy, vx: 0, vy: 0, hx, hy });
        }
      ps.current = arr;
    }, []);
    (0, import_react29.useEffect)(() => {
      build();
    }, [build]);
    const tick = (0, import_react29.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      ctx.strokeStyle = "#94a3b8";
      ctx.lineWidth = 2;
      ctx.strokeRect(6, 6, W12 - 12, H12 - 12);
      const k = temp < 4 ? 0.18 : temp < 7 ? 0.012 : 0;
      const damp = temp < 4 ? 0.86 : temp < 7 ? 0.95 : 0.997;
      const kick = temp * 0.22;
      for (const p of ps.current) {
        p.vx += (p.hx - p.x) * k + (Math.random() - 0.5) * kick;
        p.vy += (p.hy - p.y) * k + (Math.random() - 0.5) * kick;
        p.vx *= damp;
        p.vy *= damp;
        p.x += p.vx;
        p.y += p.vy;
        if (p.x < 12) {
          p.x = 12;
          p.vx = Math.abs(p.vx);
        }
        if (p.x > W12 - 12) {
          p.x = W12 - 12;
          p.vx = -Math.abs(p.vx);
        }
        if (p.y < 12) {
          p.y = 12;
          p.vy = Math.abs(p.vy);
        }
        if (p.y > H12 - 12) {
          p.y = H12 - 12;
          p.vy = -Math.abs(p.vy);
        }
        if (temp < 4)
          drawSphere(ctx, p.x, p.y, 7, "#bfdbfe", "#3b82f6", "#1e3a8a");
        else if (temp < 7)
          drawSphere(ctx, p.x, p.y, 7, "#a5f3fc", "#06b6d4", "#0e7490");
        else
          drawSphere(ctx, p.x, p.y, 7, "#fed7aa", "#f97316", "#9a3412");
      }
      raf.current = requestAnimationFrame(tick);
    }, [temp]);
    (0, import_react29.useEffect)(() => {
      if (running2) {
        if (!firstRef.current) {
          firstRef.current = true;
          onComplete?.();
        }
        raf.current = requestAnimationFrame(tick);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, tick, onComplete]);
    return /* @__PURE__ */ (0, import_jsx_runtime26.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("canvas", { ref: cv, width: W12, height: H12, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime26.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime26.jsxs)("div", { className: "flex items-center justify-center gap-2 my-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("span", { className: "text-sm text-gray-500", children: "Tr\u1EA1ng th\xE1i hi\u1EC7n t\u1EA1i:" }),
          /* @__PURE__ */ (0, import_jsx_runtime26.jsxs)("span", { className: `px-3 py-1 rounded-full font-bold text-white ${phase === "r\u1EAFn" ? "bg-blue-600" : phase === "l\u1ECFng" ? "bg-cyan-600" : "bg-orange-500"}`, children: [
            "Th\u1EC3 ",
            phase
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime26.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
          /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("span", { className: "w-24 text-gray-600", children: "Nhi\u1EC7t \u0111\u1ED9" }),
          /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("input", { type: "range", min: 1, max: 10, value: temp, onChange: (e) => setTemp(Number(e.target.value)), className: "flex-1" }),
          /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("span", { className: "w-10 text-right font-semibold", children: temp })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("button", { onClick: () => setRunning((r) => !r), className: "mt-3 inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-orange-500 text-white font-bold hover:bg-orange-600", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime26.jsxs)(import_jsx_runtime26.Fragment, { children: [
          /* @__PURE__ */ (0, import_jsx_runtime26.jsx)(Pause, { className: "w-4 h-4" }),
          " T\u1EA1m d\u1EEBng"
        ] }) : /* @__PURE__ */ (0, import_jsx_runtime26.jsxs)(import_jsx_runtime26.Fragment, { children: [
          /* @__PURE__ */ (0, import_jsx_runtime26.jsx)(Play, { className: "w-4 h-4" }),
          " Ch\u1EA1y"
        ] }) }),
        /* @__PURE__ */ (0, import_jsx_runtime26.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("h4", { className: "font-bold text-orange-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime26.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var PhaseChangeSimulation_default = PhaseChangeSimulation;

  // physics/vendor/simulations/VaporizationSimulation.tsx
  var import_react30 = __toESM(require_react());
  var import_jsx_runtime27 = __toESM(require_jsx_runtime());
  var W11 = 640;
  var H11 = 406;
  var BX0 = 120;
  var BX1 = 300;
  var BTOP = 96;
  var BBOT = 280;
  var WMAXH = 168;
  var TX = 348;
  var GX02 = 410;
  var GX12 = 624;
  var GY0 = 104;
  var GY1 = 250;
  var VaporizationSimulation = ({ title = "Qu\xE1 tr\xECnh ho\xE1 h\u01A1i (nhi\u1EC7t ho\xE1 h\u01A1i ri\xEAng)", description = "\u0110un n\u01B0\u1EDBc: nhi\u1EC7t \u0111\u1ED9 t\u0103ng t\u1EDBi 100\xB0C r\u1ED3i gi\u1EEF nguy\xEAn trong l\xFAc s\xF4i v\xE0 ho\xE1 h\u01A1i. Nhi\u1EC7t l\u01B0\u1EE3ng c\u1EA5p khi \u0111\xF3 d\xF9ng \u0111\u1EC3 ho\xE1 h\u01A1i: Q = L\xB7m.", onComplete }) => {
    const cv = useSimRef("VaporizationSimulation.cv", null);
    const raf = useSimRef("VaporizationSimulation.raf", null);
    const tRef = useSimRef("VaporizationSimulation.tRef", 0);
    const [q, setQ] = useSimState("VaporizationSimulation.q", 0);
    const [running2, setRunning] = useSimState("VaporizationSimulation.running", true);
    const qRef = useSimRef("VaporizationSimulation.qRef", q), runRef = useSimRef("VaporizationSimulation.runRef", running2);
    const firstRef = useSimRef("VaporizationSimulation.firstRef", false);
    (0, import_react30.useEffect)(() => {
      qRef.current = q;
    }, [q]);
    (0, import_react30.useEffect)(() => {
      runRef.current = running2;
    }, [running2]);
    const touch = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
    };
    const tempOf = (x) => x < 0.3 ? 25 + 75 * (x / 0.3) : x < 0.9 ? 100 : 100 + (x - 0.9) / 0.1 * 40;
    const waterFrac = (x) => x < 0.3 ? 1 : x < 0.92 ? 1 - (x - 0.3) / 0.62 * 0.96 : Math.max(0, 0.04 - (x - 0.92) / 0.08 * 0.04);
    const stateOf = (x) => x < 0.3 ? "N\u01B0\u1EDBc \u0111ang n\xF3ng l\xEAn" : x < 0.9 ? "\u0110ang s\xF4i & ho\xE1 h\u01A1i (100\xB0C)" : x < 0.99 ? "N\u01B0\u1EDBc g\u1EA7n c\u1EA1n" : "N\u01B0\u1EDBc \u0111\xE3 c\u1EA1n (kh\xF4)";
    const roundRectPath = (ctx, x, y, w, h, r) => {
      ctx.beginPath();
      ctx.moveTo(x + r, y);
      ctx.lineTo(x + w - r, y);
      ctx.quadraticCurveTo(x + w, y, x + w, y + r);
      ctx.lineTo(x + w, y + h - r);
      ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
      ctx.lineTo(x + r, y + h);
      ctx.quadraticCurveTo(x, y + h, x, y + h - r);
      ctx.lineTo(x, y + r);
      ctx.quadraticCurveTo(x, y, x + r, y);
    };
    const draw = (0, import_react30.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W11, H11);
      if (!ctx)
        return;
      const t = tRef.current, qq = qRef.current, temp = tempOf(qq), wf = waterFrac(qq), boiling = qq >= 0.3 && qq < 0.92;
      const bg = ctx.createLinearGradient(0, 0, 0, H11);
      bg.addColorStop(0, "#fbfdff");
      bg.addColorStop(1, "#eef3f8");
      ctx.fillStyle = bg;
      ctx.fillRect(0, 0, W11, H11);
      const steamN = Math.round(30 * (1 - wf) + 6);
      for (let i = 0; i < steamN; i++) {
        const ph = i * 1.9;
        const sx = BX0 + 24 + i * 47 % (BX1 - BX0 - 48) + Math.sin(t * 1.2 + ph) * 10;
        const rise = (t * 24 + i * 38) % (BTOP + 70);
        const sy = BBOT - wf * WMAXH - rise;
        if (sy < 6)
          continue;
        const a = Math.max(0, 0.42 - rise / (BTOP + 150));
        ctx.fillStyle = `rgba(186,200,214,${a})`;
        ctx.beginPath();
        ctx.arc(sx, sy, 6 + rise * 0.03, 0, Math.PI * 2);
        ctx.fill();
      }
      const waterTop = BBOT - wf * WMAXH;
      const wg = ctx.createLinearGradient(0, waterTop, 0, BBOT);
      wg.addColorStop(0, "#7dd3fc");
      wg.addColorStop(1, "#0284c7");
      ctx.fillStyle = wg;
      ctx.beginPath();
      ctx.moveTo(BX0 + 3, waterTop);
      for (let x = BX0 + 3; x <= BX1 - 3; x += 6)
        ctx.lineTo(x, waterTop + Math.sin(x * 0.08 + t * 3) * 2.5);
      ctx.lineTo(BX1 - 3, BBOT - 3);
      ctx.lineTo(BX0 + 3, BBOT - 3);
      ctx.closePath();
      ctx.fill();
      const jit = 1 + (temp - 25) / 16, cols = 7, rows = 5;
      if (wf > 0.05)
        for (let r = 0; r < rows; r++)
          for (let c = 0; c < cols; c++) {
            const bx = BX0 + 20 + c * ((BX1 - BX0 - 38) / (cols - 1));
            const by = waterTop + 16 + r * ((BBOT - waterTop - 26) / (rows - 1));
            if (by < waterTop + 8)
              continue;
            const x = bx + Math.sin(t * jit + r * 1.7 + c) * 2.6 * jit;
            const y = by + Math.cos(t * jit * 1.1 + c * 1.3) * 2.6 * jit;
            ctx.fillStyle = "#0369a1";
            ctx.beginPath();
            ctx.arc(x, y, 3.6, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = "rgba(255,255,255,0.5)";
            ctx.beginPath();
            ctx.arc(x - 1, y - 1, 1.2, 0, Math.PI * 2);
            ctx.fill();
          }
      if (boiling)
        for (let i = 0; i < 8; i++) {
          const x = BX0 + 24 + i * 61 % (BX1 - BX0 - 44);
          const prog = (t * 0.5 + i * 0.13) % 1;
          const y = BBOT - 6 - prog * (BBOT - waterTop - 10);
          ctx.strokeStyle = `rgba(255,255,255,${0.85 - prog * 0.5})`;
          ctx.lineWidth = 1.4;
          ctx.beginPath();
          ctx.arc(x, y, 2 + prog * 4, 0, Math.PI * 2);
          ctx.stroke();
        }
      ctx.strokeStyle = "rgba(148,170,190,0.9)";
      ctx.lineWidth = 3.5;
      ctx.lineJoin = "round";
      ctx.beginPath();
      ctx.moveTo(BX0 - 6, BTOP - 6);
      ctx.lineTo(BX0, BTOP);
      ctx.lineTo(BX0, BBOT);
      ctx.lineTo(BX1, BBOT);
      ctx.lineTo(BX1, BTOP);
      ctx.lineTo(BX1 + 6, BTOP - 6);
      ctx.stroke();
      ctx.strokeStyle = "rgba(255,255,255,0.6)";
      ctx.lineWidth = 5;
      ctx.beginPath();
      ctx.moveTo(BX0 + 10, BTOP + 14);
      ctx.lineTo(BX0 + 10, BBOT - 14);
      ctx.stroke();
      const cxL = (BX0 + BX1) / 2;
      ctx.fillStyle = "#94a3b8";
      ctx.fillRect(BX0 - 6, BBOT, BX1 - BX0 + 12, 4);
      const lampTop = 336, lampBot = 398, lw = 86;
      const lg = ctx.createLinearGradient(cxL - lw / 2, 0, cxL + lw / 2, 0);
      lg.addColorStop(0, "#7c8a9c");
      lg.addColorStop(0.45, "#eef2f7");
      lg.addColorStop(1, "#7c8a9c");
      ctx.fillStyle = lg;
      roundRectPath(ctx, cxL - lw / 2, lampTop, lw, lampBot - lampTop, 16);
      ctx.fill();
      ctx.fillStyle = "rgba(96,165,250,0.4)";
      roundRectPath(ctx, cxL - lw / 2 + 7, lampTop + 18, lw - 14, lampBot - lampTop - 26, 10);
      ctx.fill();
      ctx.fillStyle = "rgba(255,255,255,0.55)";
      ctx.fillRect(cxL - lw / 2 + 9, lampTop + 8, 6, lampBot - lampTop - 18);
      ctx.fillStyle = "#b45309";
      roundRectPath(ctx, cxL - 18, 320, 36, 18, 5);
      ctx.fill();
      ctx.fillStyle = "#92400e";
      ctx.fillRect(cxL - 20, 334, 40, 4);
      ctx.fillStyle = "#fde68a";
      ctx.fillRect(cxL - 3, 306, 6, 16);
      const wickY = 308, flick = Math.sin(t * 9) * 4;
      const flame = (hw, topY, col) => {
        ctx.fillStyle = col;
        ctx.beginPath();
        ctx.moveTo(cxL - hw, wickY);
        ctx.quadraticCurveTo(cxL - hw, topY + 8, cxL, topY);
        ctx.quadraticCurveTo(cxL + hw, topY + 8, cxL + hw, wickY);
        ctx.closePath();
        ctx.fill();
      };
      flame(15, BBOT - 2 - flick, "rgba(251,146,60,0.92)");
      flame(10, BBOT + 6 - flick, "rgba(251,191,36,0.95)");
      flame(5, BBOT + 16 - flick, "rgba(191,219,254,0.95)");
      const ty0 = BTOP + 4, ty1 = BBOT - 6;
      ctx.fillStyle = "#fff";
      ctx.strokeStyle = "#94a3b8";
      ctx.lineWidth = 2;
      roundRectPath(ctx, TX - 7, ty0, 14, ty1 - ty0, 7);
      ctx.fill();
      ctx.stroke();
      ctx.beginPath();
      ctx.arc(TX, ty1 + 9, 12, 0, Math.PI * 2);
      ctx.fillStyle = "#dc2626";
      ctx.fill();
      const lev = ty1 - Math.min(140, temp) / 140 * (ty1 - ty0);
      ctx.fillStyle = "#dc2626";
      ctx.fillRect(TX - 4, lev, 8, ty1 - lev);
      ctx.strokeStyle = "#94a3b8";
      ctx.fillStyle = "#64748b";
      ctx.font = "9px sans-serif";
      ctx.lineWidth = 1;
      ctx.textAlign = "right";
      for (let v = 0; v <= 140; v += 20) {
        const yy = ty1 - v / 140 * (ty1 - ty0);
        ctx.beginPath();
        ctx.moveTo(TX + 8, yy);
        ctx.lineTo(TX + 13, yy);
        ctx.stroke();
        ctx.fillText(String(v), TX + 30, yy + 3);
      }
      ctx.textAlign = "left";
      ctx.fillStyle = "#0f172a";
      ctx.font = "bold 14px sans-serif";
      ctx.fillText(`${temp.toFixed(0)}\xB0C`, TX - 14, ty0 - 8);
      const X = (x) => GX02 + x * (GX12 - GX02);
      const Y = (T03) => GY1 - Math.min(140, T03) / 140 * (GY1 - GY0);
      ctx.strokeStyle = "#e2e8f0";
      ctx.lineWidth = 1;
      for (let v = 20; v <= 140; v += 20) {
        ctx.beginPath();
        ctx.moveTo(GX02, Y(v));
        ctx.lineTo(GX12, Y(v));
        ctx.stroke();
      }
      ctx.strokeStyle = "#1e293b";
      ctx.fillStyle = "#1e293b";
      ctx.lineWidth = 1.8;
      ctx.lineJoin = "round";
      ctx.beginPath();
      ctx.moveTo(GX02, GY0 - 6);
      ctx.lineTo(GX02, GY1);
      ctx.lineTo(GX12, GY1);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(GX02, GY0 - 10);
      ctx.lineTo(GX02 - 4, GY0 - 2);
      ctx.lineTo(GX02 + 4, GY0 - 2);
      ctx.closePath();
      ctx.fill();
      ctx.beginPath();
      ctx.moveTo(GX12 + 6, GY1);
      ctx.lineTo(GX12 - 2, GY1 - 4);
      ctx.lineTo(GX12 - 2, GY1 + 4);
      ctx.closePath();
      ctx.fill();
      ctx.font = "bold 11px sans-serif";
      ctx.fillText("T (\xB0C)", GX02 - 4, GY0 - 12);
      ctx.fillText("th\u1EDDi gian (s)", GX12 - 60, GY1 + 16);
      ctx.strokeStyle = "rgba(220,38,38,0.4)";
      ctx.setLineDash([4, 3]);
      ctx.beginPath();
      ctx.moveTo(GX02, Y(100));
      ctx.lineTo(GX12, Y(100));
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#dc2626";
      ctx.font = "10px sans-serif";
      ctx.fillText("100", GX02 - 22, Y(100) + 3);
      ctx.strokeStyle = "#ea580c";
      ctx.lineWidth = 2.6;
      ctx.beginPath();
      const iMaxV = Math.max(0, Math.ceil(qq * 100));
      for (let i = 0; i <= iMaxV; i++) {
        const x = Math.min(qq, i / 100);
        const px2 = X(x), py = Y(tempOf(x));
        i === 0 ? ctx.moveTo(px2, py) : ctx.lineTo(px2, py);
      }
      ctx.stroke();
      ctx.fillStyle = "#dc2626";
      ctx.beginPath();
      ctx.arc(X(qq), Y(temp), 5, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = "#b45309";
      ctx.font = "bold 10px sans-serif";
      ctx.fillText("ho\xE1 h\u01A1i (Q = L\xB7m)", X(0.45), Y(100) - 6);
      ctx.fillStyle = "#0f172a";
      ctx.font = "bold 13px sans-serif";
      ctx.fillText(stateOf(qq), BX0 - 6, 40);
    }, []);
    const tick = (0, import_react30.useCallback)(() => {
      tRef.current += 0.05;
      if (runRef.current) {
        const nq = Math.min(1, qRef.current + 22e-4);
        qRef.current = nq;
        setQ(nq);
        if (nq >= 1)
          setRunning(false);
      }
      draw();
      raf.current = requestAnimationFrame(tick);
    }, [draw]);
    (0, import_react30.useEffect)(() => {
      raf.current = requestAnimationFrame(tick);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [tick]);
    return /* @__PURE__ */ (0, import_jsx_runtime27.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime27.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime27.jsx)("canvas", { ref: cv, width: W11, height: H11, className: "w-full rounded-lg border border-gray-100" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime27.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime27.jsxs)("label", { className: "flex items-center gap-2 text-sm mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime27.jsx)("span", { className: "w-32 text-gray-600", children: "Th\u1EDDi gian \u0111un" }),
          /* @__PURE__ */ (0, import_jsx_runtime27.jsx)("input", { type: "range", min: 0, max: 1, step: 0.01, value: q, onChange: (e) => {
            touch();
            setRunning(false);
            setQ(Number(e.target.value));
          }, className: "flex-1 accent-orange-500" }),
          /* @__PURE__ */ (0, import_jsx_runtime27.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
            Math.round(q * 100),
            "%"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime27.jsxs)("div", { className: "flex gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime27.jsx)("button", { onClick: () => {
            touch();
            if (q >= 1)
              setQ(0);
            setRunning((r) => !r);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-orange-500 text-white font-bold hover:bg-orange-600", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime27.jsxs)(import_jsx_runtime27.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime27.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime27.jsxs)(import_jsx_runtime27.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime27.jsx)(Play, { className: "w-4 h-4" }),
            " \u0110un"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime27.jsxs)("button", { onClick: () => {
            touch();
            setQ(0);
            setRunning(false);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime27.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " \u0110\u1EB7t l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime27.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime27.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime27.jsx)("h4", { className: "font-bold text-orange-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime27.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description }),
          /* @__PURE__ */ (0, import_jsx_runtime27.jsx)("p", { className: "text-[11px] text-gray-400 mt-2", children: "\u0110o\u1EA1n n\u1EB1m ngang tr\xEAn \u0111\u1ED3 th\u1ECB T\u2013Q (\u1EDF 100\xB0C): nhi\u1EC7t \u0111\u1ED9 kh\xF4ng \u0111\u1ED5i d\xF9 v\u1EABn c\u1EA5p nhi\u1EC7t \u2014 nhi\u1EC7t l\u01B0\u1EE3ng d\xF9ng \u0111\u1EC3 ho\xE1 h\u01A1i. Q = L\xB7m v\u1EDBi L l\xE0 nhi\u1EC7t ho\xE1 h\u01A1i ri\xEAng." })
        ] })
      ] })
    ] });
  };
  var VaporizationSimulation_default = VaporizationSimulation;

  // physics/vendor/simulations/NuclearReactionSimulation.tsx
  var import_react31 = __toESM(require_react());
  var import_jsx_runtime28 = __toESM(require_jsx_runtime());
  function sphere(ctx, x, y, r, hi, base, edge) {
    const g = ctx.createRadialGradient(x - r * 0.35, y - r * 0.35, r * 0.12, x, y, r);
    g.addColorStop(0, hi);
    g.addColorStop(0.45, base);
    g.addColorStop(1, edge);
    ctx.fillStyle = g;
    ctx.beginPath();
    ctx.arc(x, y, r, 0, Math.PI * 2);
    ctx.fill();
  }
  var NuclearReactionSimulation = ({ title = "Ph\u1EA3n \u1EE9ng ph\xE2n h\u1EA1ch d\xE2y chuy\u1EC1n", description = "B\u1EAFn neutron v\xE0o U-235: h\u1EA1t nh\xE2n v\u1EE1 th\xE0nh 2 m\u1EA3nh v\xE0 ph\xE1t th\xEAm neutron, g\xE2y ph\u1EA3n \u1EE9ng d\xE2y chuy\u1EC1n.", onComplete }) => {
    const cv = useSimRef("NuclearReactionSimulation.cv", null);
    const raf = useSimRef("NuclearReactionSimulation.raf", null);
    const nuc = useSimRef("NuclearReactionSimulation.nuc", []);
    const neu = useSimRef("NuclearReactionSimulation.neu", []);
    const frag = useSimRef("NuclearReactionSimulation.frag", []);
    const fisRef = useSimRef("NuclearReactionSimulation.fisRef", 0);
    const [fissions, setFissions] = useSimState("NuclearReactionSimulation.fissions", 0);
    const [speed, setSpeed] = useSimState("NuclearReactionSimulation.speed", 1);
    const speedRef = useSimRef("NuclearReactionSimulation.speedRef", 1);
    (0, import_react31.useEffect)(() => {
      speedRef.current = speed;
    }, [speed]);
    const firstRef = useSimRef("NuclearReactionSimulation.firstRef", false);
    const W12 = 560, H12 = 320, R = 14;
    const drawStatic = (0, import_react31.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      ctx.fillStyle = "#0f172a";
      ctx.fillRect(0, 0, W12, H12);
      for (const n of nuc.current) {
        if (n.alive)
          sphere(ctx, n.x, n.y, R, "#bbf7d0", "#22c55e", "#065f46");
      }
    }, []);
    const build = (0, import_react31.useCallback)(() => {
      if (raf.current) {
        cancelAnimationFrame(raf.current);
        raf.current = null;
      }
      const arr = [];
      for (let r = 0; r < 4; r++)
        for (let c = 0; c < 6; c++) {
          arr.push({ x: 130 + c * 70, y: 50 + r * 70, alive: true });
        }
      nuc.current = arr;
      neu.current = [];
      frag.current = [];
      fisRef.current = 0;
      setFissions(0);
      drawStatic();
    }, [drawStatic]);
    (0, import_react31.useEffect)(() => {
      build();
    }, [build]);
    const fire = () => {
      if (!firstRef.current) {
        firstRef.current = true;
        onComplete?.();
      }
      neu.current.push({ x: 6, y: H12 / 2 + (Math.random() - 0.5) * 60, vx: 3.2, vy: (Math.random() - 0.5) * 0.6 });
      if (!raf.current)
        raf.current = requestAnimationFrame(tick);
    };
    const tick = (0, import_react31.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx) {
        raf.current = null;
        return;
      }
      ctx.clearRect(0, 0, W12, H12);
      ctx.fillStyle = "#0f172a";
      ctx.fillRect(0, 0, W12, H12);
      for (const n of nuc.current) {
        if (!n.alive)
          continue;
        sphere(ctx, n.x, n.y, R, "#bbf7d0", "#22c55e", "#065f46");
      }
      const keep = [];
      for (const m of neu.current) {
        m.x += m.vx * speedRef.current;
        m.y += m.vy * speedRef.current;
        let hit = false;
        for (const n of nuc.current) {
          if (n.alive && Math.hypot(n.x - m.x, n.y - m.y) < R + 3) {
            n.alive = false;
            hit = true;
            fisRef.current++;
            for (let i = 0; i < 2; i++) {
              const a = Math.random() * Math.PI * 2;
              frag.current.push({ x: n.x, y: n.y, vx: Math.cos(a) * 1.8, vy: Math.sin(a) * 1.8, life: 60 });
            }
            const k = 2 + Math.round(Math.random());
            for (let i = 0; i < k; i++) {
              const a = Math.random() * Math.PI * 2;
              neu.current.push({ x: n.x, y: n.y, vx: Math.cos(a) * 3, vy: Math.sin(a) * 3 });
            }
            break;
          }
        }
        if (!hit && m.x > -10 && m.x < W12 + 10 && m.y > -10 && m.y < H12 + 10)
          keep.push(m);
        if (!hit)
          sphere(ctx, m.x, m.y, 4, "#fef9c3", "#fbbf24", "#b45309");
      }
      neu.current = keep;
      frag.current = frag.current.filter((g) => (g.life = (g.life || 0) - 1) > 0);
      for (const g of frag.current) {
        g.x += g.vx * speedRef.current;
        g.y += g.vy * speedRef.current;
        sphere(ctx, g.x, g.y, 6, "#fed7aa", "#f97316", "#9a3412");
      }
      if (fisRef.current !== fissions)
        setFissions(fisRef.current);
      if (neu.current.length || frag.current.length)
        raf.current = requestAnimationFrame(tick);
      else
        raf.current = null;
    }, [fissions]);
    (0, import_react31.useEffect)(() => () => {
      if (raf.current)
        cancelAnimationFrame(raf.current);
    }, []);
    return /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime28.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime28.jsx)("canvas", { ref: cv, width: W12, height: H12, className: "w-full rounded-lg border border-gray-100" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("label", { className: "flex items-center gap-2 text-sm mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime28.jsx)("span", { className: "w-28 text-gray-600", children: "T\u1ED1c \u0111\u1ED9" }),
          /* @__PURE__ */ (0, import_jsx_runtime28.jsx)("input", { type: "range", min: 0.4, max: 2.6, step: 0.2, value: speed, onChange: (e) => setSpeed(Number(e.target.value)), className: "flex-1" }),
          /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("span", { className: "w-12 text-right font-semibold", children: [
            speed.toFixed(1),
            "\xD7"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("div", { className: "flex items-center justify-between my-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("span", { className: "text-sm text-gray-600", children: [
            "S\u1ED1 l\u1EA7n ph\xE2n h\u1EA1ch: ",
            /* @__PURE__ */ (0, import_jsx_runtime28.jsx)("b", { className: "text-emerald-700 text-lg", children: fissions })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("div", { className: "flex gap-2", children: [
            /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("button", { onClick: fire, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-amber-500 text-white font-bold hover:bg-amber-600", children: [
              /* @__PURE__ */ (0, import_jsx_runtime28.jsx)(Zap, { className: "w-4 h-4" }),
              " B\u1EAFn neutron"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("button", { onClick: build, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
              /* @__PURE__ */ (0, import_jsx_runtime28.jsx)(RotateCcw, { className: "w-4 h-4" }),
              " \u0110\u1EB7t l\u1EA1i"
            ] })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime28.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime28.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime28.jsx)("h4", { className: "font-bold text-emerald-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime28.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var NuclearReactionSimulation_default = NuclearReactionSimulation;

  // physics/vendor/simulations/RadioactiveDecaySimulation.tsx
  var import_react32 = __toESM(require_react());
  var import_jsx_runtime29 = __toESM(require_jsx_runtime());
  function sphere2(ctx, x, y, r, hi, base, edge) {
    const g = ctx.createRadialGradient(x - r * 0.35, y - r * 0.35, r * 0.12, x, y, r);
    g.addColorStop(0, hi);
    g.addColorStop(0.45, base);
    g.addColorStop(1, edge);
    ctx.fillStyle = g;
    ctx.beginPath();
    ctx.arc(x, y, r, 0, Math.PI * 2);
    ctx.fill();
  }
  var RadioactiveDecaySimulation = ({ title = "M\xF4 ph\u1ECFng Ph\xF3ng x\u1EA1", description = "K\xE9o thanh tr\u01B0\u1EE3t th\u1EDDi gian t \u0111\u1EC3 quan s\xE1t s\u1ED1 h\u1EA1t nh\xE2n c\xF2n l\u1EA1i N = N\u2080\xB72^(\u2212t/T) gi\u1EA3m d\u1EA7n v\xE0 \u0111i\u1EC3m ch\u1EA1y tr\xEAn \u0111\u1ED3 th\u1ECB.", onComplete }) => {
    const cv = useSimRef("RadioactiveDecaySimulation.cv", null);
    const raf = useSimRef("RadioactiveDecaySimulation.raf", null);
    const lastRef = useSimRef("RadioactiveDecaySimulation.lastRef", null);
    const nucleiRef = useSimRef("RadioactiveDecaySimulation.nucleiRef", []);
    const [halfLife, setHalfLife] = useSimState("RadioactiveDecaySimulation.halfLife", 5);
    const [n0, setN0] = useSimState("RadioactiveDecaySimulation.n0", 200);
    const [t, setT] = useSimState("RadioactiveDecaySimulation.t", 0);
    const [running2, setRunning] = useSimState("RadioactiveDecaySimulation.running", false);
    const firstRef = useSimRef("RadioactiveDecaySimulation.firstRef", false);
    const W12 = 720, H12 = 300;
    const Tmax = halfLife * 6;
    const build = (0, import_react32.useCallback)((count) => {
      const cols = Math.ceil(Math.sqrt(count));
      nucleiRef.current = Array.from({ length: count }, (_, i) => ({
        x: i % cols,
        y: Math.floor(i / cols),
        th: Math.random()
      }));
    }, []);
    (0, import_react32.useEffect)(() => {
      build(n0);
      setT(0);
      setRunning(false);
    }, [n0, build]);
    const remaining = Math.round(n0 * Math.pow(2, -t / halfLife));
    const draw = (0, import_react32.useCallback)(() => {
      const ctx = prepCanvas(cv.current, W12, H12);
      if (!ctx)
        return;
      ctx.clearRect(0, 0, W12, H12);
      const f = 1 - Math.pow(2, -t / halfLife);
      const nuclei = nucleiRef.current;
      const cols = Math.ceil(Math.sqrt(nuclei.length));
      const gridW = Math.floor(W12 * 0.48);
      const cell = Math.min((gridW - 20) / cols, (H12 - 20) / cols);
      const r = Math.max(2.5, cell * 0.34);
      for (const nu of nuclei) {
        const cx = 12 + nu.x * cell + cell / 2;
        const cy = 12 + nu.y * cell + cell / 2;
        if (nu.th < f) {
          ctx.fillStyle = "rgba(148,163,184,0.28)";
          ctx.beginPath();
          ctx.arc(cx, cy, r * 0.8, 0, Math.PI * 2);
          ctx.fill();
        } else
          sphere2(ctx, cx, cy, r, "#bbf7d0", "#10b981", "#065f46");
      }
      const gx0 = gridW + 30, gy0 = H12 - 30, gw = W12 - gx0 - 18, gh = H12 - 52;
      ctx.strokeStyle = "#94a3b8";
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(gx0, 14);
      ctx.lineTo(gx0, gy0);
      ctx.lineTo(gx0 + gw, gy0);
      ctx.stroke();
      ctx.fillStyle = "#475569";
      ctx.font = "bold 12px sans-serif";
      ctx.fillText("N", gx0 - 6, 12);
      ctx.fillText("t (s)", gx0 + gw - 26, gy0 + 18);
      ctx.strokeStyle = "rgba(148,163,184,0.45)";
      ctx.setLineDash([4, 3]);
      ctx.lineWidth = 1;
      for (let k = 1; k <= 6; k++) {
        const px3 = gx0 + k * halfLife / Tmax * gw;
        ctx.beginPath();
        ctx.moveTo(px3, 14);
        ctx.lineTo(px3, gy0);
        ctx.stroke();
      }
      ctx.setLineDash([]);
      ctx.strokeStyle = "#2563eb";
      ctx.lineWidth = 3.5;
      ctx.beginPath();
      const tCur = Math.min(t, Tmax);
      const iMax = Math.ceil(tCur / Tmax * 120);
      for (let i = 0; i <= iMax; i++) {
        const tt = Math.min(tCur, i / 120 * Tmax);
        const n = n0 * Math.pow(2, -tt / halfLife);
        const px3 = gx0 + tt / Tmax * gw, py2 = gy0 - n / n0 * gh;
        i === 0 ? ctx.moveTo(px3, py2) : ctx.lineTo(px3, py2);
      }
      ctx.stroke();
      const px2 = gx0 + Math.min(t / Tmax, 1) * gw;
      const py = gy0 - remaining / n0 * gh;
      ctx.strokeStyle = "rgba(239,68,68,0.5)";
      ctx.lineWidth = 1.5;
      ctx.setLineDash([3, 3]);
      ctx.beginPath();
      ctx.moveTo(px2, gy0);
      ctx.lineTo(px2, py);
      ctx.moveTo(gx0, py);
      ctx.lineTo(px2, py);
      ctx.stroke();
      ctx.setLineDash([]);
      ctx.fillStyle = "#ef4444";
      ctx.beginPath();
      ctx.arc(px2, py, 6, 0, Math.PI * 2);
      ctx.fill();
      ctx.strokeStyle = "#fff";
      ctx.lineWidth = 2;
      ctx.stroke();
    }, [t, halfLife, n0, remaining, Tmax]);
    (0, import_react32.useEffect)(() => {
      draw();
    }, [draw]);
    const tick = (0, import_react32.useCallback)((ts) => {
      if (lastRef.current == null)
        lastRef.current = ts;
      const dt = Math.min(0.05, (ts - lastRef.current) / 1e3);
      lastRef.current = ts;
      setT((prev) => {
        const nt = prev + dt;
        if (nt >= Tmax) {
          setRunning(false);
          if (!firstRef.current) {
            firstRef.current = true;
            onComplete?.();
          }
          return Tmax;
        }
        return nt;
      });
      raf.current = requestAnimationFrame(tick);
    }, [Tmax, onComplete]);
    (0, import_react32.useEffect)(() => {
      if (running2) {
        lastRef.current = null;
        raf.current = requestAnimationFrame(tick);
      } else if (raf.current)
        cancelAnimationFrame(raf.current);
      return () => {
        if (raf.current)
          cancelAnimationFrame(raf.current);
      };
    }, [running2, tick]);
    return /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "sim-layout", children: [
      /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("div", { className: "sim-visuals", children: /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("canvas", { ref: cv, width: W12, height: H12, className: "w-full rounded-lg border border-gray-100 bg-slate-50" }) }),
      /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "sim-controls", children: [
        /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "grid grid-cols-2 md:grid-cols-4 gap-2 my-3 text-center", children: [
          /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "p-2 rounded-lg bg-emerald-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("div", { className: "text-lg font-black text-emerald-700", children: remaining }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("div", { className: "text-[11px] text-gray-500", children: "C\xF2n l\u1EA1i N" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "p-2 rounded-lg bg-slate-100", children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("div", { className: "text-lg font-black text-slate-600", children: n0 - remaining }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("div", { className: "text-[11px] text-gray-500", children: "\u0110\xE3 ph\xE2n r\xE3" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "p-2 rounded-lg bg-blue-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "text-lg font-black text-blue-700", children: [
              t.toFixed(1),
              "s"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("div", { className: "text-[11px] text-gray-500", children: "Th\u1EDDi gian t" })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "p-2 rounded-lg bg-amber-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "text-lg font-black text-amber-700", children: [
              halfLife,
              "s"
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("div", { className: "text-[11px] text-gray-500", children: "Chu k\xEC b\xE1n r\xE3 T" })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "space-y-2", children: [
          /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("span", { className: "w-28 text-gray-600 font-semibold", children: "\u23F1 Th\u1EDDi gian t" }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("input", { type: "range", min: 0, max: Tmax, step: 0.1, value: t, onChange: (e) => {
              setRunning(false);
              setT(Number(e.target.value));
            }, className: "flex-1 accent-red-500" }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("span", { className: "w-14 text-right font-semibold", children: [
              t.toFixed(1),
              "s"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("span", { className: "w-28 text-gray-600", children: "Chu k\xEC b\xE1n r\xE3 T" }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("input", { type: "range", min: 1, max: 15, step: 1, value: halfLife, disabled: running2, onChange: (e) => {
              setHalfLife(Number(e.target.value));
              setT(0);
            }, className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("span", { className: "w-14 text-right font-semibold", children: [
              halfLife,
              "s"
            ] })
          ] }),
          /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("label", { className: "flex items-center gap-2 text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("span", { className: "w-28 text-gray-600", children: "S\u1ED1 h\u1EA1t ban \u0111\u1EA7u N\u2080" }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("input", { type: "range", min: 50, max: 400, step: 50, value: n0, disabled: running2, onChange: (e) => setN0(Number(e.target.value)), className: "flex-1" }),
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("span", { className: "w-14 text-right font-semibold", children: n0 })
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("div", { className: "flex items-center gap-2 mt-3", children: [
          /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("button", { onClick: () => {
            if (t >= Tmax)
              setT(0);
            setRunning((r) => !r);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg bg-emerald-600 text-white font-bold hover:bg-emerald-700", "data-nb-play": "", children: running2 ? /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)(import_jsx_runtime29.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)(Pause, { className: "w-4 h-4" }),
            " T\u1EA1m d\u1EEBng"
          ] }) : /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)(import_jsx_runtime29.Fragment, { children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)(Play, { className: "w-4 h-4" }),
            " Ch\u1EA1y t\u1EF1 \u0111\u1ED9ng"
          ] }) }),
          /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("button", { onClick: () => {
            setRunning(false);
            setT(0);
          }, className: "inline-flex items-center gap-1.5 px-4 py-2 rounded-lg border border-gray-200 text-gray-600 font-bold hover:bg-gray-50", children: [
            /* @__PURE__ */ (0, import_jsx_runtime29.jsx)(RotateCcw, { className: "w-4 h-4" }),
            " \u0110\u1EB7t l\u1EA1i"
          ] })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime29.jsxs)("details", { className: "sim-help", children: [
          /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("summary", { children: "H\u01B0\u1EDBng d\u1EABn m\xF4 ph\u1ECFng" }),
          /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("h4", { className: "font-bold text-emerald-800", children: title }),
          /* @__PURE__ */ (0, import_jsx_runtime29.jsx)("p", { className: "text-sm text-gray-500 mb-3", children: description })
        ] })
      ] })
    ] });
  };
  var RadioactiveDecaySimulation_default = RadioactiveDecaySimulation;

  // physics/generated/flat.tsx
  mount({ "force-vector": ForceVectorSimulation, "inclined-plane": InclinedPlaneSimulation_default, "collision": CollisionSimulation_default, "circular-motion": CircularMotionSimulation_default, "motion-graphs": MotionGraphsSimulation_default, "displacement-time": DisplacementTimeSimulation_default, "vt-graph": InteractiveVTGraph_default, "hooke-spring": HookeSpringSimulation_default, "spring-oscillator": SpringOscillatorSimulation_default, "simple-pendulum": SimplePendulumSimulation_default, "energy-oscillation": EnergyOscillationSimulation_default, "two-oscillations": TwoOscillationsSimulation_default, "driven-resonance": DrivenResonanceSimulation_default, "wave-propagation": WavePropagationSimulation_default, "wave-interference": WaveInterferenceSimulation_default, "standing-wave": StandingWaveSimulation_default, "coulomb": CoulombForceSimulation_default, "electric-field": ElectricFieldSimulation_default, "resistance-ohm": ResistanceOhmSimulation_default, "emf-measurement": EMFMeasurementSimulation_default, "gas-law": GasLawSimulation_default, "gas-particles": GasParticleSimulation_default, "heating-curve": HeatingCurveSimulation_default, "specific-heat": SpecificHeatSimulation_default, "phase-change": PhaseChangeSimulation_default, "vaporization": VaporizationSimulation_default, "nuclear-reaction": NuclearReactionSimulation_default, "radioactive-decay": RadioactiveDecaySimulation_default });
})();
/*! Bundled license information:

react/cjs/react.production.min.js:
  (**
   * @license React
   * react.production.min.js
   *
   * Copyright (c) Facebook, Inc. and its affiliates.
   *
   * This source code is licensed under the MIT license found in the
   * LICENSE file in the root directory of this source tree.
   *)

scheduler/cjs/scheduler.production.min.js:
  (**
   * @license React
   * scheduler.production.min.js
   *
   * Copyright (c) Facebook, Inc. and its affiliates.
   *
   * This source code is licensed under the MIT license found in the
   * LICENSE file in the root directory of this source tree.
   *)

react-dom/cjs/react-dom.production.min.js:
  (**
   * @license React
   * react-dom.production.min.js
   *
   * Copyright (c) Facebook, Inc. and its affiliates.
   *
   * This source code is licensed under the MIT license found in the
   * LICENSE file in the root directory of this source tree.
   *)

react/cjs/react-jsx-runtime.production.min.js:
  (**
   * @license React
   * react-jsx-runtime.production.min.js
   *
   * Copyright (c) Facebook, Inc. and its affiliates.
   *
   * This source code is licensed under the MIT license found in the
   * LICENSE file in the root directory of this source tree.
   *)
*/
