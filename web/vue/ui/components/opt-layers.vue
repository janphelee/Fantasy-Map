<template>
  <div>
    <!-- <el-divider></el-divider> -->
    <span>图层显示以及排序:</span>
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
  </div>
</template>

<script>
import { CheckboxGroup, CheckboxButton, Row, Col } from "element-ui";
import ElDivider from "element-ui/lib/divider";

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
    this.$api.get_layers((d) => {
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
    this.$api.get_on_layers((d) => {
      this.checkboxGroup1 = d;
    });
  },
  methods: {
    onChange(d) {
      this.$api.on_layers_toggled(d, (d) => {});
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
