<template>
  <el-tabs v-model="activeName" @tab-click="handleClick">
    <el-tab-pane label="图层" name="1st">
      <opt-layers />
    </el-tab-pane>
    <el-tab-pane label="风格" name="2nd">风格调整</el-tab-pane>
    <el-tab-pane label="参数" name="3rd">
      <opt-options />
    </el-tab-pane>
    <el-tab-pane label="工具" name="4th">可视工具</el-tab-pane>
    <el-tab-pane label="关于" name="5ve">有点时间</el-tab-pane>
  </el-tabs>
</template>

<script>
import { Tabs, TabPane } from "element-ui";
import OptLayers from "./components/opt-layers";
import OptOptions from "./components/opt-options";

const components = {
  "el-tabs": Tabs,
  "el-tab-pane": TabPane,
  OptLayers,
  OptOptions,
};

export default {
  components,
  data() {
    return {
      activeName: "3rd",
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
