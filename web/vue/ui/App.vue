<template>
  <el-tabs v-model="activeName"
           @tab-click="handleClick">
    <el-tab-pane label="图层"
                 name="first">
      <el-checkbox-group v-model="checkboxGroup1">
        <el-col span="4"
                class="el-col"
                v-for="(layer,i) in layers"
                :key="i-layer">
          <el-checkbox-button :label="layer">{{layer}}</el-checkbox-button>
        </el-col>
      </el-checkbox-group>
    </el-tab-pane>
    <el-tab-pane label="风格"
                 name="second">配置管理</el-tab-pane>
    <el-tab-pane label="参数"
                 name="third">角色管理</el-tab-pane>
    <el-tab-pane label="工具"
                 name="fourth">定时任务补偿</el-tab-pane>
    <el-tab-pane label="关于"
                 name="five">定时任务补偿</el-tab-pane>
  </el-tabs>
</template>

<script>
import {
  Tabs,
  TabPane,
  CheckboxGroup,
  CheckboxButton,
  // Checkbox,
  Col,
  Row,
} from "element-ui";

const components = {
  "el-tabs": Tabs,
  "el-tab-pane": TabPane,
  "el-checkbox-group": CheckboxGroup,
  "el-checkbox-button": CheckboxButton,
  'el-col': Col,
  'el-row': Row,
}

const layers = [
  "底纹",
  "等高图",
  "气候带",
  "单元格",
  "网格",
  "坐标",
  "风向",
  "河流",
  "地貌",
  "信仰",
  "文化",
  "势力",
  "领地",
  "焦点区域",
  "边界",
  "路线",
  "温度",
  "人口",
  "降雨量",
  "区域名称",
  "据点图标",
  "著名地标",
  "直尺",
  "比例尺",
]

export default {
  components,
  data () {
    return {
      activeName: "first",
      checkboxGroup1: [],
      layers: layers,
    };
  },
  methods: {
    handleClick (tab, event) {
      console.log(tab, event);
    },
  },
};
</script>

<style scoped>
.el-col {
  margin: 12px;
}
</style>
