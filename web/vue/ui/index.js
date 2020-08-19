// import Vue from 'vue'
import App from './App'
// import ElementUI from 'element-ui'

import axios from 'axios'
// import md5 from 'js-md5'

// Vue.use(ElementUI)
import 'element-ui/lib/theme-chalk/index.css';

axios.defaults.baseURL = '/api';
// 配置拦截器携带token
axios.interceptors.request.use(config => {
  config.headers["token"] = "xxxx";
  return config
}, error => {
  return Promise.reject(error);
});

Vue.config.productionTip = false;
Vue.prototype.$http = axios;
// Vue.prototype.$md5 = md5;

/* eslint-disable no-new */
new Vue({
  el: '#app',
  render: h => h(App)
})
