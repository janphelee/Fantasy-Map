// import Vue from 'vue'
import App from './App'
import ElementUI from 'element-ui'

// import axios from 'axios'
// import md5 from 'js-md5'

// Vue.use(ElementUI)
import 'element-ui/lib/theme-chalk/index.css';


// Vue.config.productionTip = false;
// Vue.prototype.$http = axios;
// Vue.prototype.$md5 = md5;

// axios.defaults.baseURL = "http://192.168.0.101:8000/api/toodo/";
// axios.defaults.baseURL = "http://tddev.toodo.com.cn/tdmgrsrv/api/toodo/";
// 配置拦截器携带token
// axios.interceptors.request.use(config => {
//   // 本地token
//   // config.headers["token"] = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwOlwvXC8xOTIuMTY4LjAuMTAxOjgwMDBcL2FwaVwvdG9vZG9cL3VzZXJcL2xvZ2luIiwiaWF0IjoxNTg1NjE3NTUzLCJuYmYiOjE1ODU2MTc1NTMsImp0aSI6IkdnVGs1azQxR3NHZ2o1ajEiLCJzdWIiOjEzLCJwcnYiOiJlMzNlNmU5MzE4NWFkZWI4ODQ1ZWZkMmMwMTE3ZWMzYTI1Mzc5MzA1In0.vo48xpvTkA7-c4Phzg1YtomLv51iHf6_ZdRnyLhpj80";
//   // 测试服token
//   config.headers["token"] = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwOlwvXC90ZGRldi50b29kby5jb20uY25cL3RkbWdyc3J2XC9hcGlcL3Rvb2RvXC91c2VyXC9sb2dpbiIsImlhdCI6MTU4NzAyNTE5NiwibmJmIjoxNTg3MDI1MTk2LCJqdGkiOiI1UmZjUDRMdVZ4ZEliNUVKIiwic3ViIjoxMywicHJ2IjoiZTMzZTZlOTMxODVhZGViODg0NWVmZDJjMDExN2VjM2EyNTM3OTMwNSJ9.fXXRK5e82pTbck7W4hwyOB4lAj56OtqGBtfsvW6QKs0";
//   return config
// }, error => {
//   return Promise.reject(error);
// });


/* eslint-disable no-new */
new Vue({
  el: '#app',
  render: h => h(App)
})
