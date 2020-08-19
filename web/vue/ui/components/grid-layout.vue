<template>
  <div>
    <el-row v-for="i in rows" :key="i">
      <el-col>{{items[i*3+0]}}</el-col>
      <el-col>{{items[i*3+1]}}</el-col>
      <el-col>{{items[i*3+2]}}</el-col>
    </el-row>
  </div>
</template>

<script>
import { Row, Col } from "element-ui";
const components = {
  "el-row": Row,
  "el-col": Col,
};
export default {
  components,
  props: {
    col: 3,
    items: [],
  },
  data() {
    return {};
  },
  computed: {
    rows: function () {
      let a = Math.ceil(this.items.length / this.col);
      let b = [];
      for (let i = 0; i < a; ++i) b.push(i);
      return b;
    },
  },
};
</script>