<template>
  <div>
    <span>地图设置:</span>

    <opt-map-size :label="options['opt_map_size']"
                  v-model="setting.opt_map_size">
      <span> - - - </span>
      <el-button type="success"
                 @click="generate">生成地图</el-button>
    </opt-map-size>

    <opt-map-slider v-for="(v,k) in limits"
                    v-model="setting[k]"
                    :label="options[k]"
                    :min="v[0]"
                    :max="v[1]"
                    :step="v[2]"
                    :key="k" />
  </div>
</template>

<script>
import ElRow from "element-ui/lib/row";
import ElCol from "element-ui/lib/col";
import ElButton from "element-ui/lib/button";
import ElInput from "element-ui/lib/input";
import ElSlider from "element-ui/lib/slider";

import OptMapSize from './options/opt-map-size'
import OptMapSlider from './options/opt-map-slider'

const components = {
  ElRow, ElCol, ElButton, ElInput, ElSlider,
  OptMapSize, OptMapSlider
};
export default {
  components,
  data () {
    return {
      options: {},
      setting: {
        opt_map_size: {w: 1153, h: 717},
        opt_map_seed: 1,
      },
      arrays: {
        opt_map_template: [],
        opt_map_cultures_set: [],
      },
      limits: {
        // opt_map_size: [ 1, 65536, 1 ],
        // opt_map_seed: [ 0, 4294967295, 1 ],
        opt_map_points_n: [ 10, 100, 10 ],
        opt_map_cultures_n: [ 1, 32, 1 ],
        opt_map_states_n: [ 0, 99, 1 ],
        opt_map_provinces_ratio: [ 0, 100, 1 ],
        opt_map_size_variety: [ 0, 10, 0.01 ],
        opt_map_growth_rate: [ 0.1, 2, 0.01 ],
        opt_map_towns_n: [ 0, 1000, 1 ],
        opt_map_religions_n: [ 0, 50, 1 ],
      },
      generatorSettings: {
        onloadBehavior: 0,
        interfaceSize: [ 1, 0.6, 3 ],
        tooltipSize: [ 14, 4, 32 ],
        transparency: [ 5, 0, 100 ],
        zoomExtent: {min: 1, max: 20},
      },
    };
  },
  created () {
    this.$api.get_options((d) => {
      this.options = d;
    });
    this.$api.get_on_options((d) => {
      this.setting = d;
    });
  },
  methods: {
    generate () {
      this.$api.on_options_toggled(this.setting, d => {});
    },
  },
};
</script>

<style scoped>
</style>

