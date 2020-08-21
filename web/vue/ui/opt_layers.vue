<template>
  <div>
    <span>青春是一个短暂的美梦, 当你醒来时, 它早已消失无踪</span>
    <el-divider></el-divider>

    <el-checkbox-group v-model="checkboxGroup1" @change="onChange">
      <el-row v-for="(row,i) in layout" :key="`row-${i}`" gutter="8">
        <el-col
          :xs="8"
          :sm="7"
          :md="6"
          :lg="4"
          :xl="3"
          v-for="(col,j) in row"
          :key="`col-${j}`"
          class="el-col"
        >
          <el-checkbox-button :label="col">{{layers[col]}}</el-checkbox-button>
        </el-col>
      </el-row>
    </el-checkbox-group>

    <el-divider></el-divider>
    <span>少量的邪恶足以抵消全部高贵的品质, 害得人声名狼藉</span>
  </div>
</template>

<script>
import { CheckboxGroup, CheckboxButton, Row, Col } from "element-ui";
import ElDivider from "element-ui/lib/divider";
import api from "./api-layers";

const components = {
  "el-checkbox-group": CheckboxGroup,
  "el-checkbox-button": CheckboxButton,
  "el-row": Row,
  "el-col": Col,
  ElDivider,
};

export default {
  components,
  data() {
    return {
      checkboxGroup1: [],
      layers: [],
      layout: [],
    };
  },
  created() {
    api.axiso = this.$http;
    api.get_layers((d) => {
      this.layers = d;

      let layout = [];
      d.forEach((l, i) => {
        let r = Math.floor(i / 3);
        let c = i % 3;
        if (layout[r] == null) layout[r] = [];
        layout[r].push(i);
      });
      this.layout = layout;
    });
    api.get_on_layers((d) => {
      this.checkboxGroup1 = d;
    });
  },
  methods: {
    onChange(d) {
      api.on_layers_toggled(d, (d) => {});
    },
  },
};
</script>

<style scoped>
.el-col {
  margin-top: 4px;
  margin-bottom: 4px;
}
</style>

<style>
.el-checkbox-button,
.el-checkbox-button__inner {
  width: 100%;
}
</style>
