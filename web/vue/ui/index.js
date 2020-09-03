import App from './App'
import axios from 'axios'
import api from './api/index'

import 'element-ui/lib/theme-chalk/index.css';

axios.defaults.baseURL = '/api';
// 配置拦截器携带token
axios.interceptors.request.use(config => {
  config.headers["token"] = "xxxx";
  return config
}, error => {
  return Promise.reject(error);
});

api.axios = axios;

Vue.config.productionTip = false;
Vue.prototype.$api = api;

new Vue({
  el: '#app',
  render: h => h(App)
})
