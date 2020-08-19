<template>
  <el-tabs v-model="activeName" @tab-click="handleClick">
    <el-tab-pane label="图层" name="first">
      <opt-layers />
    </el-tab-pane>
    <el-tab-pane label="风格" name="second">配置管理</el-tab-pane>
    <el-tab-pane label="参数" name="third">角色管理</el-tab-pane>
    <el-tab-pane label="工具" name="fourth">定时任务补偿</el-tab-pane>
    <el-tab-pane label="关于" name="five">定时任务补偿</el-tab-pane>
  </el-tabs>
</template>

<script>
import { Tabs, TabPane } from "element-ui";
import OptLayers from "./opt_layers";

const components = {
  "el-tabs": Tabs,
  "el-tab-pane": TabPane,
  OptLayers,
};

export default {
  components,
  data() {
    return {
      activeName: "first",
    };
  },
  methods: {
    handleClick(tab, event) {
      // console.log(tab, event);
    },
  },
};
</script>

<style scoped>
</style>
