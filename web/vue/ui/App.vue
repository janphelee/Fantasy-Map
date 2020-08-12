<template>
  <el-tabs v-model="activeName" @tab-click="handleClick">
    <el-tab-pane label="用户管理" name="first">
      <el-checkbox-group v-model="checkboxGroup1">
        <el-checkbox-button v-for="city in cities" :label="city" :key="city">{{city}}</el-checkbox-button>
      </el-checkbox-group>
    </el-tab-pane>
    <el-tab-pane label="配置管理" name="second">配置管理</el-tab-pane>
    <el-tab-pane label="角色管理" name="third">角色管理</el-tab-pane>
    <el-tab-pane label="定时任务补偿" name="fourth">定时任务补偿</el-tab-pane>
  </el-tabs>
</template>

<script>
import {
  Tabs,
  TabPane,
  CheckboxGroup,
  CheckboxButton,
  // Checkbox,
} from "element-ui";
import "element-ui/lib/theme-chalk/tabs.css";
import "element-ui/lib/theme-chalk/tab-pane.css";
import "element-ui/lib/theme-chalk/checkbox.css";
import "element-ui/lib/theme-chalk/checkbox-button.css";
import "element-ui/lib/theme-chalk/checkbox-group.css";

const cityOptions = ["上海", "北京", "广州", "深圳"];

export default {
  components: {
    "el-tabs": Tabs,
    "el-tab-pane": TabPane,
    "el-checkbox-group": CheckboxGroup,
    "el-checkbox-button": CheckboxButton,
  },
  data() {
    return {
      activeName: "second",
      checkboxGroup1: ["上海"],
      cities: cityOptions,
    };
  },
  methods: {
    handleClick(tab, event) {
      console.log(tab, event);
    },
  },
};
</script>

<style>
</style>
